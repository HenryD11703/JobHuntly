using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Interfaces;

/// <summary>
/// Entities returned by this repository are tracked: services mutate them and call
/// <see cref="SaveAsync"/>, so related changes (a status change and its timeline event) are
/// written in one transaction.
/// </summary>
public interface IJobApplicationRepository
{
    /// <summary>All of the user's applications with interviews (and debriefs), events and offers loaded.</summary>
    Task<IEnumerable<JobApplication>> GetAllByUserIdAsync(Guid userId);

    Task<JobApplication?> GetByIdAsync(Guid id, Guid userId);
    Task<JobApplication> AddAsync(JobApplication jobApplication);
    Task SaveAsync();
    Task DeleteAsync(JobApplication jobApplication);
}
