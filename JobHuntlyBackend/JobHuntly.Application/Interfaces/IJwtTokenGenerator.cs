using System;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        /// <param name="lifetime">How long the token lasts. Defaults to JwtSettings:ExpiryMinutes.</param>
        string GenerateToken(User user, TimeSpan? lifetime = null);
    }
}
