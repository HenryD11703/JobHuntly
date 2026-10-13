using System.ComponentModel.DataAnnotations;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.DTOs;

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
    [Required, StringLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string PositionTitle { get; set; } = string.Empty;

    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Backlog;

    [StringLength(5000)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? SalaryRange { get; set; }

    [Url, StringLength(2000)]
    public string? JobPostUrl { get; set; }
}

public class UpdateJobApplicationStatusDto
{
    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus Status { get; set; }
}

public class UpdateJobApplicationDto
{
    [StringLength(5000)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? SalaryRange { get; set; }

    [Url, StringLength(2000)]
    public string? JobPostUrl { get; set; }

    [StringLength(1000)]
    public string? RejectionReason { get; set; }
}
