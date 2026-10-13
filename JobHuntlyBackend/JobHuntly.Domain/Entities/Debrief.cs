namespace JobHuntly.Domain.Entities;

/// <summary>What happened in an interview, kept as structured data so insights can use it later.</summary>
public class Debrief
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public int Rating { get; set; }
    public string RawNotes { get; set; } = string.Empty;
    public List<string> QuestionsAsked { get; set; } = new();
    public List<string> WentWell { get; set; } = new();
    public List<string> WentPoorly { get; set; } = new();
    public List<string> FollowUps { get; set; } = new();
    public DateTime SavedAt { get; set; }

    public Interview Interview { get; set; } = null!;
}
