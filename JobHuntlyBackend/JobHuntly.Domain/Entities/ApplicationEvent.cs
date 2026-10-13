namespace JobHuntly.Domain.Entities;

public enum EventType
{
    Created = 0,
    StatusChanged = 1,
    InterviewAdded = 2,
    FollowedUp = 3,
    DebriefSaved = 4,
    OfferAdded = 5
}

/// <summary>
/// One entry in an application's timeline. It is the source of truth for funnel and
/// time-in-stage insights, so the history survives later status changes.
/// </summary>
public class ApplicationEvent
{
    public Guid Id { get; set; }
    public Guid JobApplicationId { get; set; }
    public EventType Type { get; set; }
    public DateTime OccurredAt { get; set; }
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus? ToStatus { get; set; }
    public string? Note { get; set; }

    public JobApplication JobApplication { get; set; } = null!;
}
