using JobHuntly.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobHuntly.Application.Interfaces
{
    public interface IJobApplicationRepository
    {
        Task<IEnumerable<JobApplication>> GetAllByUserIdAsync(Guid userId);
        Task<JobApplication?> GetByIdAsync(Guid id, Guid userId);
        Task<JobApplication> AddAsync(JobApplication jobApplication);
        Task UpdateAsync(JobApplication jobApplication);
        Task DeleteAsync(JobApplication jobApplication);
        Task AddInterviewAsync(Interview interview);
    }
}
