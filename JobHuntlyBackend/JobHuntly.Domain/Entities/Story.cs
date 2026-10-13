namespace JobHuntly.Domain.Entities;

/// <summary>A STAR story (Situation, Task, Action, Result) the user can reuse across interviews.</summary>
public class Story
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Situation { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
