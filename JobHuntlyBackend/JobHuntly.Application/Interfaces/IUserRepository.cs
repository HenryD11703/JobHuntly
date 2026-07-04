using JobHuntly.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace JobHuntly.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> AddAsync(User user);
    }
}
