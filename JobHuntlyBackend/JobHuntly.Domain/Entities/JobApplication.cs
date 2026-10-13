namespace JobHuntly.Domain.Entities;

public class JobApplication
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Backlog;
    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string? SalaryRange { get; set; }
    public string? JobPostUrl { get; set; }
    public string? RejectionReason { get; set; }
    public ApplicationSource? Source { get; set; }
    public string? JobPostText { get; set; }
    public string? Location { get; set; }
    public bool IsRemote { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    public ICollection<ApplicationEvent> Events { get; set; } = new List<ApplicationEvent>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
