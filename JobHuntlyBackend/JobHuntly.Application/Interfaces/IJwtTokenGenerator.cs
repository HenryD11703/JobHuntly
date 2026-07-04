using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
