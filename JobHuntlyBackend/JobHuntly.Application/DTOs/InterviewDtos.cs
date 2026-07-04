using System;

namespace JobHuntly.Application.DTOs
{
    public class InterviewDto
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public DateTime InterviewDate { get; set; }
        public string RoundType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CreateInterviewDto
    {
        public DateTime InterviewDate { get; set; }
        public string RoundType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
