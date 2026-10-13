namespace JobHuntly.Domain.Entities;

public class Interview
{
    public Guid Id { get; set; }
    public Guid JobApplicationId { get; set; }
    public DateTime InterviewDate { get; set; }
    public InterviewRound RoundType { get; set; }
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public string? Notes { get; set; }

    /// <summary>Ids of checklist items the user ticked (see ChecklistCatalog).</summary>
    public List<string> ChecklistDone { get; set; } = new();

    /// <summary>AI-suggested questions saved as JSON so they are not regenerated on every view.</summary>
    public string? PrepQuestionsJson { get; set; }

    public JobApplication JobApplication { get; set; } = null!;
    public Debrief? Debrief { get; set; }
}
