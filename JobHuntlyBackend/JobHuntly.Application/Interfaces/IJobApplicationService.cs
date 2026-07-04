using JobHuntly.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobHuntly.Application.Interfaces
{
    public interface IJobApplicationService
    {
        Task<IEnumerable<JobApplicationDto>> GetAllApplicationsAsync(Guid userId);
        Task<JobApplicationDto> CreateApplicationAsync(CreateJobApplicationDto dto, Guid userId);
        Task<JobApplicationDto> UpdateApplicationStatusAsync(Guid id, UpdateJobApplicationStatusDto dto, Guid userId);
        Task<JobApplicationDto> UpdateApplicationAsync(Guid id, UpdateJobApplicationDto dto, Guid userId);
        Task DeleteApplicationAsync(Guid id, Guid userId);
        Task<InterviewDto> AddInterviewAsync(Guid jobApplicationId, CreateInterviewDto dto, Guid userId);
    }
}
