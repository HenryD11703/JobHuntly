using JobHuntly.Application.DTOs;

namespace JobHuntly.Application.Ai;

/// <summary>Talks to the language model. Implemented by the Groq client; faked in tests.</summary>
public interface IAiClient
{
    /// <summary>False when the server has no API key. Checked before any quota is used.</summary>
    bool IsConfigured { get; }

    /// <summary>Sends a system and a user prompt and returns the model's raw text (expected to be JSON).</summary>
    Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}

/// <summary>Daily AI allowance per user. All methods are atomic.</summary>
public interface IAiQuota
{
    /// <summary>Uses one unit if the user is under <paramref name="limit"/>. Returns false when the allowance is used up.</summary>
    Task<bool> TryConsumeAsync(Guid userId, DateOnly day, int limit);

    /// <summary>Gives a unit back (the call failed, so it should not count).</summary>
    Task ReleaseAsync(Guid userId, DateOnly day);

    Task<int> GetUsedAsync(Guid userId, DateOnly day);
}

/// <summary>Daily limits. Registered users get <see cref="Registered"/>, demo sandbox users get <see cref="Demo"/>.</summary>
public sealed record AiLimits(int Registered, int Demo);

public interface IAiAssistantService
{
    Task<AiStatusDto> GetStatusAsync(Guid userId);
    Task<JobPostDraftDto> ExtractJobPostAsync(string text, Guid userId, CancellationToken ct);
    Task<PrepResultDto> GetPrepQuestionsAsync(Guid applicationId, Guid interviewId, bool regenerate, Guid userId, CancellationToken ct);
    Task<DebriefDraftDto> StructureDebriefAsync(Guid applicationId, Guid interviewId, string rawNotes, Guid userId, CancellationToken ct);
}
