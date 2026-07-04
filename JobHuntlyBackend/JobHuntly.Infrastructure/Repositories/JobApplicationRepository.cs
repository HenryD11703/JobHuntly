using JobHuntly.Application.Interfaces;
using JobHuntly.Domain.Entities;
using JobHuntly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobHuntly.Infrastructure.Repositories
{
    public class JobApplicationRepository : IJobApplicationRepository
    {
        private readonly AppDbContext _context;

        public JobApplicationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<JobApplication> AddAsync(JobApplication jobApplication)
        {
            await _context.JobApplications.AddAsync(jobApplication);
            await _context.SaveChangesAsync();
            return jobApplication;
        }

        public async Task DeleteAsync(JobApplication jobApplication)
        {
            _context.JobApplications.Remove(jobApplication);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.JobApplications
                .Include(j => j.Interviews)
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.AppliedDate)
                .ToListAsync();
        }

        public async Task<JobApplication?> GetByIdAsync(Guid id, Guid userId)
        {
            return await _context.JobApplications
                .Include(j => j.Interviews)
                .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
        }

        public async Task UpdateAsync(JobApplication jobApplication)
        {
            _context.JobApplications.Update(jobApplication);
            await _context.SaveChangesAsync();
        }

        public async Task AddInterviewAsync(Interview interview)
        {
            await _context.Interviews.AddAsync(interview);
            await _context.SaveChangesAsync();
        }
    }
}
