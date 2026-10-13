using System.Text.Json;
using JobHuntly.Application.DTOs;
using JobHuntly.Application.Exceptions;
using JobHuntly.Application.Interfaces;

namespace JobHuntly.Application.Ai;

/// <summary>
/// The three AI-assisted features. Every method returns a DRAFT: nothing is saved here except the
/// generated interview questions (so they are not regenerated on every view). The deterministic
/// parts of the app never depend on this class.
/// </summary>
public class AiAssistantService(
    IAiClient ai,
    IAiQuota quota,
    AiLimits limits,
    IUserRepository users,
    IJobApplicationRepository applications,
    IStoryRepository stories,
    TimeProvider clock) : IAiAssistantService
{
    private static readonly JsonSerializerOptions StoreOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiStatusDto> GetStatusAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        var today = Today();
        return new AiStatusDto
        {
            Enabled = ai.IsConfigured,
            DailyLimit = LimitFor(user),
            UsedToday = await quota.GetUsedAsync(userId, today),
            ResetsAt = ResetsAt(today)
        };
    }

    public Task<JobPostDraftDto> ExtractJobPostAsync(string text, Guid userId, CancellationToken ct) =>
        GuardedAsync(userId, () => AskAsync(
            Prompts.ExtractSystem, Prompts.ExtractUser(text), AiOutputParser.ParseJobPost, ct));

    public async Task<PrepResultDto> GetPrepQuestionsAsync(Guid applicationId, Guid interviewId, bool regenerate, Guid userId, CancellationToken ct)
    {
        var application = await applications.GetByIdAsync(applicationId, userId)
                          ?? throw new NotFoundException("Application not found.");
        var interview = application.Interviews.FirstOrDefault(i => i.Id == interviewId)
                        ?? throw new NotFoundException("Interview not found.");

        // Cache: a saved set is returned without touching the AI or the allowance.
        if (!regenerate && ReadPrep(interview.PrepQuestionsJson) is { } cached)
            return new PrepResultDto { Prep = cached, FromCache = true };

        var userStories = await stories.GetAllByUserIdAsync(userId);
        var known = userStories.ToDictionary(s => s.Id, s => s.Title);

        var questions = await GuardedAsync(userId, () => AskAsync(
            Prompts.PrepSystem(interview.RoundType),
            Prompts.PrepUser(application, userStories),
            raw => AiOutputParser.ParsePrepQuestions(raw, known),
            ct));

        var prep = new PrepQuestionsDto { GeneratedAt = clock.GetUtcNow().UtcDateTime, Questions = questions };
        interview.PrepQuestionsJson = JsonSerializer.Serialize(prep, StoreOptions);
        await applications.SaveAsync();

        return new PrepResultDto { Prep = prep, FromCache = false };
    }

    public async Task<DebriefDraftDto> StructureDebriefAsync(Guid applicationId, Guid interviewId, string rawNotes, Guid userId, CancellationToken ct)
    {
        var application = await applications.GetByIdAsync(applicationId, userId)
                          ?? throw new NotFoundException("Application not found.");
        if (application.Interviews.All(i => i.Id != interviewId))
            throw new NotFoundException("Interview not found.");

        return await GuardedAsync(userId, () => AskAsync(
            Prompts.DebriefSystem, Prompts.DebriefUser(rawNotes), AiOutputParser.ParseDebrief, ct));
    }

    /// <summary>Reads the stored prep questions, or null when there are none (or the stored JSON is unreadable).</summary>
    public static PrepQuestionsDto? ReadPrep(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<PrepQuestionsDto>(json, StoreOptions); }
        catch (JsonException) { return null; }
    }

    // ---- plumbing ----

    /// <summary>Asks the model; if the answer does not validate, asks once more, then gives up.</summary>
    private async Task<T> AskAsync<T>(string system, string user, Func<string, T> parse, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var raw = await ai.CompleteJsonAsync(system, user, ct);
            try
            {
                return parse(raw);
            }
            catch (AiOutputException) when (attempt == 1)
            {
                // one retry
            }
            catch (AiOutputException)
            {
                throw new AiInvalidOutputException();
            }
        }
    }

    /// <summary>Checks the key and the allowance first, uses one unit, and gives it back if the call fails.</summary>
    private async Task<T> GuardedAsync<T>(Guid userId, Func<Task<T>> call)
    {
        if (!ai.IsConfigured)
            throw new ExternalServiceException("AI features are not available on this server. You can fill this in manually.", "ai_not_configured");

        var user = await users.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        var today = Today();

        if (!await quota.TryConsumeAsync(userId, today, LimitFor(user)))
            throw new QuotaExceededException(ResetsAt(today));

        try
        {
            return await call();
        }
        catch
        {
            await quota.ReleaseAsync(userId, today);
            throw;
        }
    }

    private int LimitFor(Domain.Entities.User user) => user.IsDemo ? limits.Demo : limits.Registered;

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private static DateTime ResetsAt(DateOnly today) =>
        today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
