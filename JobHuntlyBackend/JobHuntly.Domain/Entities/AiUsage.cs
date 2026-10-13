namespace JobHuntly.Domain.Entities;

/// <summary>How many AI calls a user has made on a given UTC day (for the daily allowance).</summary>
public class AiUsage
{
    public Guid UserId { get; set; }
    public DateOnly Day { get; set; }
    public int Count { get; set; }

    public User User { get; set; } = null!;
}
