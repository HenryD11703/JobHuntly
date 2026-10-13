using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Services;

public class AnalyticsService(IJobApplicationRepository repository) : IAnalyticsService
{
    public async Task<AnalyticsSummaryDto> GetSummaryAsync(Guid userId)
    {
        var applications = (await repository.GetAllByUserIdAsync(userId)).ToList();
        var total = applications.Count;

        if (total == 0) return new AnalyticsSummaryDto();

        var rejected = applications.Count(a => a.Status == ApplicationStatus.Rejected);
        var offers = applications.Count(a => a.Status == ApplicationStatus.Offer);
        var active = total - rejected - offers;

        // Applications that reached at least "Interviewing" or beyond, or have interviews recorded
        var interviewed = applications.Count(a =>
            a.Status == ApplicationStatus.Interviewing ||
            a.Status == ApplicationStatus.TechnicalTest ||
            a.Status == ApplicationStatus.Offer ||
            a.Interviews.Any());

        return new AnalyticsSummaryDto
        {
            TotalApplications = total,
            ActiveApplications = active,
            RejectedApplications = rejected,
            OffersReceived = offers,
            InterviewConversionRate = Math.Round((double)interviewed / total * 100, 1)
        };
    }

    public async Task<InsightsDto> GetInsightsAsync(Guid userId) =>
        InsightsCalculator.Calculate((await repository.GetAllByUserIdAsync(userId)).ToList());
}
