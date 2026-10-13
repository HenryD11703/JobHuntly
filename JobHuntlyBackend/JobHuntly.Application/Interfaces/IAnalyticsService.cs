using JobHuntly.Application.DTOs;

namespace JobHuntly.Application.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsSummaryDto> GetSummaryAsync(Guid userId);
    Task<InsightsDto> GetInsightsAsync(Guid userId);
}
