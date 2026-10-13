using JobHuntly.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace JobHuntly.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto request);
        Task<AuthResponseDto> LoginAsync(LoginDto request);
        Task<ProfileDto> GetProfileAsync(Guid userId);
    }
}
