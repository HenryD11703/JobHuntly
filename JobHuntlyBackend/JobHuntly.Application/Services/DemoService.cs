using JobHuntly.Application.Demo;
using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;

namespace JobHuntly.Application.Services;

public interface IDemoService
{
    /// <summary>Creates an isolated sandbox user filled with fictional data and returns a session for it.</summary>
    Task<AuthResponseDto> CreateSandboxAsync();
}

public class DemoService(IUserRepository users, IJwtTokenGenerator tokens, TimeProvider clock) : IDemoService
{
    public async Task<AuthResponseDto> CreateSandboxAsync()
    {
        // Nobody knows this password: a sandbox can only be entered through the demo button, and a
        // sandbox email is never reachable through the normal login form.
        var unguessable = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var hash = BCrypt.Net.BCrypt.HashPassword(unguessable, workFactor: 4);
        var email = $"demo-{Guid.NewGuid():N}@demo.jobhuntly.invalid";

        var user = DemoSeed.Build(email, hash, clock.GetUtcNow().UtcDateTime);
        await users.AddAsync(user);   // one save: the whole graph is inserted in a single transaction

        return new AuthResponseDto
        {
            Token = tokens.GenerateToken(user, DemoSeed.Lifetime),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
