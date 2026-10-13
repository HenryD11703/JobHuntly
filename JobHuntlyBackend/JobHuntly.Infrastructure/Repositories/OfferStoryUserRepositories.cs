using JobHuntly.Application.Interfaces;
using JobHuntly.Domain.Entities;
using JobHuntly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobHuntly.Infrastructure.Repositories;

public class OfferRepository(AppDbContext context) : IOfferRepository
{
    public Task<List<Offer>> GetAllByUserIdAsync(Guid userId) =>
        context.Offers
            .Include(o => o.JobApplication)
            .Where(o => o.JobApplication.UserId == userId)
            .ToListAsync();

    public Task<Offer?> GetByIdAsync(Guid id, Guid userId) =>
        context.Offers
            .Include(o => o.JobApplication)
            .FirstOrDefaultAsync(o => o.Id == id && o.JobApplication.UserId == userId);

    public async Task AddAsync(Offer offer)
    {
        await context.Offers.AddAsync(offer);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Offer offer)
    {
        context.Offers.Remove(offer);
        await context.SaveChangesAsync();
    }
}

public class StoryRepository(AppDbContext context) : IStoryRepository
{
    public Task<List<Story>> GetAllByUserIdAsync(Guid userId) =>
        context.Stories.Where(s => s.UserId == userId).OrderByDescending(s => s.CreatedAt).ToListAsync();

    public Task<Story?> GetByIdAsync(Guid id, Guid userId) =>
        context.Stories.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

    public async Task AddAsync(Story story)
    {
        await context.Stories.AddAsync(story);
        await context.SaveChangesAsync();
    }

    public async Task SaveAsync() => await context.SaveChangesAsync();

    public async Task DeleteAsync(Story story)
    {
        context.Stories.Remove(story);
        await context.SaveChangesAsync();
    }
}
