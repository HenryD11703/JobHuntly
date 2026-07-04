using JobHuntly.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace JobHuntly.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task<AnalyticsSummaryDto> GetSummaryAsync(Guid userId);
    }
}
