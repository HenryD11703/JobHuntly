using JobHuntly.Application.Demo;
using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobHuntly.Api.Controllers;

public class AuthController(IAuthService authService, IDemoService demo, IConfiguration configuration, IWebHostEnvironment env)
    : ApiControllerBase
{
    public const string CookieName = "jh_token";

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto request)
    {
        var result = await authService.RegisterAsync(request);
        SetAuthCookie(result.Token);
        return Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
    {
        var result = await authService.LoginAsync(request);
        SetAuthCookie(result.Token);
        return Ok(result);
    }

    /// <summary>Creates a private sandbox with fictional data (deleted after 24 h) and signs the visitor into it.</summary>
    [HttpPost("demo")]
    [EnableRateLimiting("demo")]
    public async Task<ActionResult<AuthResponseDto>> Demo()
    {
        var result = await demo.CreateSandboxAsync();
        SetAuthCookie(result.Token, DemoSeed.Lifetime);
        return Ok(result);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName, CookieOptions(null));
        return NoContent();
    }

    /// <summary>Lets the frontend ask "am I signed in?" without ever reading the token.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> Me() => Ok(await authService.GetProfileAsync(UserId));

    private void SetAuthCookie(string token, TimeSpan? lifetime = null)
    {
        var minutes = configuration.GetValue("JwtSettings:ExpiryMinutes", 120);
        Response.Cookies.Append(CookieName, token, CookieOptions(lifetime ?? TimeSpan.FromMinutes(minutes)));
    }

    private CookieOptions CookieOptions(TimeSpan? maxAge) => new()
    {
        HttpOnly = true,
        // Browsers accept Secure cookies on http://localhost, but keep dev simple and testable.
        Secure = !env.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = maxAge
    };
}
