using JobHuntly.Application.DTOs;
using JobHuntly.Application.Exceptions;
using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Services;

public class JobApplicationService(IJobApplicationRepository repository, TimeProvider clock) : IJobApplicationService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<IEnumerable<JobApplicationDto>> GetAllApplicationsAsync(Guid userId)
    {
        var applications = await repository.GetAllByUserIdAsync(userId);
        return applications.Select(Mapper.ToDto).ToList();
    }

    public async Task<JobApplicationDto> GetApplicationAsync(Guid id, Guid userId) =>
        Mapper.ToDto(await FindAsync(id, userId));

    public async Task<JobApplicationDto> CreateApplicationAsync(CreateJobApplicationDto dto, Guid userId)
    {
        var now = Now;
        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyName = dto.CompanyName.Trim(),
            PositionTitle = dto.PositionTitle.Trim(),
            Status = dto.Status,
            AppliedDate = now,
            Source = dto.Source,
            Notes = dto.Notes,
            SalaryRange = dto.SalaryRange,
            JobPostUrl = dto.JobPostUrl,
            JobPostText = dto.JobPostText,
            Location = dto.Location,
            IsRemote = dto.IsRemote
        };
        application.Events.Add(NewEvent(EventType.Created, now, toStatus: dto.Status));

        await repository.AddAsync(application);
        return Mapper.ToDto(application);
    }

    public async Task<JobApplicationDto> UpdateApplicationStatusAsync(Guid id, UpdateJobApplicationStatusDto dto, Guid userId)
    {
        var application = await FindAsync(id, userId);
        var now = Now;

        if (dto.Status != application.Status)
        {
            var firstTimeApplied = dto.Status == ApplicationStatus.Applied
                                   && !application.Events.Any(e => e.ToStatus == ApplicationStatus.Applied);
            var reason = dto.Status == ApplicationStatus.Rejected ? Clean(dto.RejectionReason) : null;

            application.Events.Add(NewEvent(EventType.StatusChanged, now, application.Status, dto.Status, reason));
            application.Status = dto.Status;
            if (firstTimeApplied) application.AppliedDate = now;
            if (dto.Status == ApplicationStatus.Rejected) application.RejectionReason = reason ?? application.RejectionReason;
            else application.RejectionReason = null;
        }
        else if (dto.Status == ApplicationStatus.Rejected && !string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            application.RejectionReason = Clean(dto.RejectionReason);
        }

        await repository.SaveAsync();
        return Mapper.ToDto(application);
    }

    public async Task<JobApplicationDto> UpdateApplicationAsync(Guid id, UpdateJobApplicationDto dto, Guid userId)
    {
        var application = await FindAsync(id, userId);

        application.Notes = dto.Notes;
        application.SalaryRange = dto.SalaryRange;
        application.JobPostUrl = dto.JobPostUrl;
        application.JobPostText = dto.JobPostText;
        application.RejectionReason = dto.RejectionReason;
        application.Source = dto.Source;
        application.Location = dto.Location;
        application.IsRemote = dto.IsRemote;

        await repository.SaveAsync();
        return Mapper.ToDto(application);
    }

    public async Task DeleteApplicationAsync(Guid id, Guid userId) =>
        await repository.DeleteAsync(await FindAsync(id, userId));

    public async Task<IEnumerable<ApplicationEventDto>> GetTimelineAsync(Guid id, Guid userId)
    {
        var application = await FindAsync(id, userId);
        // Newest first. Events can share a timestamp (e.g. created and moved in the same instant), so the
        // event type is a stable tie-breaker: Created always sorts as the oldest.
        return application.Events
            .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Type)
            .Select(Mapper.ToDto).ToList();
    }

    public async Task<ApplicationEventDto> AddFollowUpAsync(Guid id, FollowUpDto dto, Guid userId)
    {
        var application = await FindAsync(id, userId);
        var followUp = NewEvent(EventType.FollowedUp, Now, note: Clean(dto.Note));
        application.Events.Add(followUp);
        await repository.SaveAsync();
        return Mapper.ToDto(followUp);
    }

    public async Task<InterviewDto> AddInterviewAsync(Guid jobApplicationId, CreateInterviewDto dto, Guid userId)
    {
        var application = await FindAsync(jobApplicationId, userId);

        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            JobApplicationId = application.Id,
            InterviewDate = UtcDate.Normalize(dto.InterviewDate),
            RoundType = dto.RoundType,
            Status = InterviewStatus.Scheduled,
            Notes = dto.Notes
        };
        application.Interviews.Add(interview);
        application.Events.Add(NewEvent(EventType.InterviewAdded, Now, note: dto.RoundType.ToString()));

        await repository.SaveAsync();
        return Mapper.ToDto(interview);
    }

    public async Task<InterviewDto> UpdateInterviewAsync(Guid jobApplicationId, Guid interviewId, UpdateInterviewDto dto, Guid userId)
    {
        var interview = await FindInterviewAsync(jobApplicationId, interviewId, userId);

        if (dto.InterviewDate.HasValue) interview.InterviewDate = UtcDate.Normalize(dto.InterviewDate.Value);
        if (dto.Status.HasValue) interview.Status = dto.Status.Value;
        if (dto.Notes is not null) interview.Notes = dto.Notes;

        await repository.SaveAsync();
        return Mapper.ToDto(interview);
    }

    public async Task<InterviewDto> ToggleChecklistItemAsync(Guid jobApplicationId, Guid interviewId, ToggleChecklistItemDto dto, Guid userId)
    {
        var interview = await FindInterviewAsync(jobApplicationId, interviewId, userId);

        if (!ChecklistCatalog.Contains(interview.RoundType, dto.ItemId))
            throw new ValidationException($"'{dto.ItemId}' is not a checklist item for a {interview.RoundType} round.");

        var done = interview.ChecklistDone.ToList();
        if (dto.Done && !done.Contains(dto.ItemId)) done.Add(dto.ItemId);
        if (!dto.Done) done.Remove(dto.ItemId);
        interview.ChecklistDone = done;

        await repository.SaveAsync();
        return Mapper.ToDto(interview);
    }

    public async Task<InterviewDto> SaveDebriefAsync(Guid jobApplicationId, Guid interviewId, SaveDebriefDto dto, Guid userId)
    {
        var application = await FindAsync(jobApplicationId, userId);
        var interview = application.Interviews.FirstOrDefault(i => i.Id == interviewId)
                        ?? throw new NotFoundException("Interview not found.");
        var now = Now;

        if (interview.Status == InterviewStatus.Cancelled)
            throw new ValidationException("A cancelled interview cannot have a debrief.");
        if (interview.Status == InterviewStatus.Scheduled && interview.InterviewDate > now)
            throw new ValidationException("This interview has not happened yet.");

        var isNew = interview.Debrief is null;
        var debrief = interview.Debrief ?? new Debrief { Id = Guid.NewGuid(), InterviewId = interview.Id };
        debrief.Rating = dto.Rating;
        debrief.RawNotes = dto.RawNotes;
        debrief.QuestionsAsked = CleanList(dto.QuestionsAsked);
        debrief.WentWell = CleanList(dto.WentWell);
        debrief.WentPoorly = CleanList(dto.WentPoorly);
        debrief.FollowUps = CleanList(dto.FollowUps);
        debrief.SavedAt = now;

        interview.Status = InterviewStatus.Done;
        if (isNew)
        {
            interview.Debrief = debrief;
            application.Events.Add(NewEvent(EventType.DebriefSaved, now, note: interview.RoundType.ToString()));
        }

        await repository.SaveAsync();
        return Mapper.ToDto(interview);
    }

    private async Task<JobApplication> FindAsync(Guid id, Guid userId) =>
        await repository.GetByIdAsync(id, userId) ?? throw new NotFoundException("Application not found.");

    private async Task<Interview> FindInterviewAsync(Guid applicationId, Guid interviewId, Guid userId)
    {
        var application = await FindAsync(applicationId, userId);
        return application.Interviews.FirstOrDefault(i => i.Id == interviewId)
               ?? throw new NotFoundException("Interview not found.");
    }

    private static ApplicationEvent NewEvent(EventType type, DateTime at, ApplicationStatus? from = null,
        ApplicationStatus? toStatus = null, string? note = null) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        OccurredAt = at,
        FromStatus = from,
        ToStatus = toStatus,
        Note = note
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> CleanList(IEnumerable<string> items)
    {
        var cleaned = items.Select(i => i?.Trim() ?? string.Empty).Where(i => i.Length > 0).ToList();
        if (cleaned.Any(i => i.Length > 500))
            throw new ValidationException("Each debrief item must be 500 characters or fewer.");
        return cleaned;
    }
}
