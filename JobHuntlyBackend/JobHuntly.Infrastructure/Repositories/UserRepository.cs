using JobHuntly.Application.Interfaces;
using JobHuntly.Domain.Entities;
using JobHuntly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobHuntly.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User> AddAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    public Task<User?> GetUserByEmailAsync(string email) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task SaveAsync() => await context.SaveChangesAsync();
}
