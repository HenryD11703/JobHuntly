using JobHuntly.Domain.Entities;

namespace JobHuntly.Tests.Unit;

/// <summary>Small helpers to build domain objects for calculator tests.</summary>
public static class Builders
{
    public static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    public static JobApplication App(
        ApplicationStatus status = ApplicationStatus.Applied,
        DateTime? appliedAt = null,
        ApplicationSource? source = null,
        string? rejectionReason = null)
    {
        var at = appliedAt ?? Now.AddDays(-1);
        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            CompanyName = "Acme",
            PositionTitle = "Engineer",
            Status = status,
            AppliedDate = at,
            Source = source,
            RejectionReason = rejectionReason
        };
        return app;
    }

    /// <summary>Adds a status-change event to the application's timeline and moves its status.</summary>
    public static JobApplication Moved(this JobApplication app, ApplicationStatus to, DateTime at)
    {
        var from = app.Events.LastOrDefault(e => e.ToStatus.HasValue)?.ToStatus;
        app.Events.Add(new ApplicationEvent
        {
            Id = Guid.NewGuid(),
            JobApplicationId = app.Id,
            Type = app.Events.Count == 0 ? EventType.Created : EventType.StatusChanged,
            OccurredAt = at,
            FromStatus = from,
            ToStatus = to
        });
        app.Status = to;
        return app;
    }

    public static JobApplication WithEvent(this JobApplication app, EventType type, DateTime at)
    {
        app.Events.Add(new ApplicationEvent { Id = Guid.NewGuid(), JobApplicationId = app.Id, Type = type, OccurredAt = at });
        return app;
    }

    public static Interview AddInterview(
        this JobApplication app,
        DateTime date,
        InterviewRound round = InterviewRound.Technical,
        InterviewStatus status = InterviewStatus.Scheduled,
        bool withDebrief = false,
        params string[] checklistDone)
    {
        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            JobApplicationId = app.Id,
            InterviewDate = date,
            RoundType = round,
            Status = status,
            ChecklistDone = checklistDone.ToList()
        };
        if (withDebrief) interview.Debrief = new Debrief { Id = Guid.NewGuid(), InterviewId = interview.Id, Rating = 3 };
        app.Interviews.Add(interview);
        return interview;
    }
}
