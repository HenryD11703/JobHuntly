using JobHuntly.Domain.Entities;
using System;

namespace JobHuntly.Application.DTOs
{
    public class JobApplicationDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public DateTime AppliedDate { get; set; }
        public string? Notes { get; set; }
        public string? SalaryRange { get; set; }
        public string? JobPostUrl { get; set; }
        public string? RejectionReason { get; set; }
        public IEnumerable<InterviewDto> Interviews { get; set; } = new List<InterviewDto>();
    }

    public class CreateJobApplicationDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Backlog;
        public string? Notes { get; set; }
        public string? SalaryRange { get; set; }
        public string? JobPostUrl { get; set; }
    }

    public class UpdateJobApplicationStatusDto
    {
        public ApplicationStatus Status { get; set; }
    }

    public class UpdateJobApplicationDto
    {
        public string? Notes { get; set; }
        public string? SalaryRange { get; set; }
        public string? JobPostUrl { get; set; }
        public string? RejectionReason { get; set; }
    }
}
