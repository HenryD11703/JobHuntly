using JobHuntly.Application.Interfaces;
using JobHuntly.Domain.Entities;
using JobHuntly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobHuntly.Infrastructure.Repositories;

public class JobApplicationRepository(AppDbContext context) : IJobApplicationRepository
{
    private IQueryable<JobApplication> WithDetails() => context.JobApplications
        .Include(j => j.Interviews).ThenInclude(i => i.Debrief)
        .Include(j => j.Events)
        .Include(j => j.Offers)
        .AsSplitQuery();

    public async Task<IEnumerable<JobApplication>> GetAllByUserIdAsync(Guid userId) =>
        await WithDetails()
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.AppliedDate)
            .ToListAsync();

    public async Task<JobApplication?> GetByIdAsync(Guid id, Guid userId) =>
        await WithDetails().FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);

    public async Task<JobApplication> AddAsync(JobApplication jobApplication)
    {
        await context.JobApplications.AddAsync(jobApplication);
        await context.SaveChangesAsync();
        return jobApplication;
    }

    public async Task SaveAsync() => await context.SaveChangesAsync();

    public async Task DeleteAsync(JobApplication jobApplication)
    {
        context.JobApplications.Remove(jobApplication);
        await context.SaveChangesAsync();
    }
}
