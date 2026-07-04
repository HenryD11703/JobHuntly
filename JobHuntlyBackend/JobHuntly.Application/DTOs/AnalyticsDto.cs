namespace JobHuntly.Application.DTOs
{
    public class AnalyticsSummaryDto
    {
        public int TotalApplications { get; set; }
        public int ActiveApplications { get; set; }
        public int RejectedApplications { get; set; }
        public int OffersReceived { get; set; }
        public double InterviewConversionRate { get; set; } // Percentage of applications that led to interviews
    }
}
