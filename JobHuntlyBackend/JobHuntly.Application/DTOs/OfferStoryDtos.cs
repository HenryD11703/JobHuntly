using System.ComponentModel.DataAnnotations;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.DTOs;

public class CreateOfferDto
{
    [Range(0.01, 100000000)]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(Currency))]
    public Currency Currency { get; set; }

    [EnumDataType(typeof(PayPeriod))]
    public PayPeriod Period { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class OfferDto
{
    public Guid Id { get; set; }
    public Guid JobApplicationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public PayPeriod Period { get; set; }
    public string? Notes { get; set; }

    /// <summary>Per month, USD, using the user's saved rates.</summary>
    public decimal MonthlyUsd { get; set; }

    /// <summary>Per month, GTQ, using the user's saved rates.</summary>
    public decimal MonthlyGtq { get; set; }
}

public class OfferComparisonDto
{
    public RatesDto Rates { get; set; } = new();
    public List<OfferDto> Offers { get; set; } = new();
}

public class RatesDto
{
    /// <summary>Quetzales for 1 US dollar. 1 to 20 keeps typos out; real values sit near 7.7.</summary>
    [Range(1, 20)]
    public decimal UsdToGtq { get; set; }

    /// <summary>Quetzales for 1 euro.</summary>
    [Range(1, 30)]
    public decimal EurToGtq { get; set; }
}

public class SuggestedRatesDto
{
    public decimal UsdToGtq { get; set; }
    public decimal EurToGtq { get; set; }
    public DateTime ProviderUpdatedAt { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string AttributionUrl { get; set; } = string.Empty;
}

public class StoryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Situation { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SaveStoryDto
{
    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Situation { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Task { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Result { get; set; } = string.Empty;

    [MaxLength(10)]
    public List<string> Tags { get; set; } = new();
}
