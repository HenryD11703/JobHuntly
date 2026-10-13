namespace JobHuntly.Domain.Entities;

public enum Currency
{
    USD = 0,
    GTQ = 1,
    EUR = 2
}

public enum PayPeriod
{
    Monthly = 0,
    Annual = 1
}

public class Offer
{
    public Guid Id { get; set; }
    public Guid JobApplicationId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public PayPeriod Period { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public JobApplication JobApplication { get; set; } = null!;
}
