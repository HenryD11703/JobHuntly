using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using JobHuntly.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JobHuntly.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IJobApplicationRepository _repository;

        public AnalyticsService(IJobApplicationRepository repository)
        {
            _repository = repository;
        }

        public async Task<AnalyticsSummaryDto> GetSummaryAsync(Guid userId)
        {
            var applications = await _repository.GetAllByUserIdAsync(userId);
            var total = applications.Count();

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

            var conversionRate = Math.Round((double)interviewed / total * 100, 1);

            return new AnalyticsSummaryDto
            {
                TotalApplications = total,
                ActiveApplications = active,
                RejectedApplications = rejected,
                OffersReceived = offers,
                InterviewConversionRate = conversionRate
            };
        }
    }
}
