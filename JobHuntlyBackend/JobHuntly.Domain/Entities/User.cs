namespace JobHuntly.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Exchange rates the user trusts, used by the offer comparator. Editable at any time.</summary>
    public decimal UsdToGtq { get; set; } = 7.64m;
    public decimal EurToGtq { get; set; } = 8.54m;

    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    public ICollection<Story> Stories { get; set; } = new List<Story>();
}
