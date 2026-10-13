using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using JobHuntly.Domain.Entities;
using JobHuntly.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobHuntly.Application.Services
{
    public class JobApplicationService : IJobApplicationService
    {
        private readonly IJobApplicationRepository _repository;

        public JobApplicationService(IJobApplicationRepository repository)
        {
            _repository = repository;
        }

        public async Task<JobApplicationDto> CreateApplicationAsync(CreateJobApplicationDto dto, Guid userId)
        {
            var application = new JobApplication
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyName = dto.CompanyName,
                PositionTitle = dto.PositionTitle,
                Status = dto.Status,
                AppliedDate = DateTime.UtcNow,
                Notes = dto.Notes,
                SalaryRange = dto.SalaryRange,
                JobPostUrl = dto.JobPostUrl
            };

            await _repository.AddAsync(application);
            return MapToDto(application);
        }

        public async Task DeleteApplicationAsync(Guid id, Guid userId)
        {
            var application = await _repository.GetByIdAsync(id, userId);
            if (application == null)
            {
                throw new NotFoundException("Application not found.");
            }

            await _repository.DeleteAsync(application);
        }

        public async Task<IEnumerable<JobApplicationDto>> GetAllApplicationsAsync(Guid userId)
        {
            var applications = await _repository.GetAllByUserIdAsync(userId);
            return applications.Select(MapToDto);
        }

        public async Task<JobApplicationDto> UpdateApplicationStatusAsync(Guid id, UpdateJobApplicationStatusDto dto, Guid userId)
        {
            var application = await _repository.GetByIdAsync(id, userId);
            if (application == null)
            {
                throw new NotFoundException("Application not found.");
            }

            application.Status = dto.Status;
            await _repository.UpdateAsync(application);

            return MapToDto(application);
        }

        public async Task<JobApplicationDto> UpdateApplicationAsync(Guid id, UpdateJobApplicationDto dto, Guid userId)
        {
            var application = await _repository.GetByIdAsync(id, userId);
            if (application == null)
            {
                throw new NotFoundException("Application not found.");
            }

            application.Notes = dto.Notes;
            application.SalaryRange = dto.SalaryRange;
            application.JobPostUrl = dto.JobPostUrl;
            application.RejectionReason = dto.RejectionReason;

            await _repository.UpdateAsync(application);

            return MapToDto(application);
        }

        public async Task<InterviewDto> AddInterviewAsync(Guid jobApplicationId, CreateInterviewDto dto, Guid userId)
        {
            var application = await _repository.GetByIdAsync(jobApplicationId, userId);
            if (application == null)
            {
                throw new NotFoundException("Application not found.");
            }

            var interview = new Interview
            {
                Id = Guid.NewGuid(),
                JobApplicationId = jobApplicationId,
                InterviewDate = dto.InterviewDate,
                RoundType = dto.RoundType,
                Notes = dto.Notes
            };

            await _repository.AddInterviewAsync(interview);

            return new InterviewDto
            {
                Id = interview.Id,
                JobApplicationId = interview.JobApplicationId,
                InterviewDate = interview.InterviewDate,
                RoundType = interview.RoundType,
                Notes = interview.Notes
            };
        }

        private static JobApplicationDto MapToDto(JobApplication entity)
        {
            return new JobApplicationDto
            {
                Id = entity.Id,
                CompanyName = entity.CompanyName,
                PositionTitle = entity.PositionTitle,
                AppliedDate = entity.AppliedDate,
                Status = entity.Status,
                Notes = entity.Notes,
                SalaryRange = entity.SalaryRange,
                JobPostUrl = entity.JobPostUrl,
                RejectionReason = entity.RejectionReason,
                Interviews = entity.Interviews.Select(i => new InterviewDto
                {
                    Id = i.Id,
                    JobApplicationId = i.JobApplicationId,
                    InterviewDate = i.InterviewDate,
                    RoundType = i.RoundType,
                    Notes = i.Notes
                }).ToList()
            };
        }
    }
}
