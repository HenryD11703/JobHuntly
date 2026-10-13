using JobHuntly.Application.DTOs;
using JobHuntly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobHuntly.Api.Controllers;

public class AuthController(IAuthService authService, IConfiguration configuration, IWebHostEnvironment env)
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

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName, CookieOptions(null));
        return NoContent();
    }

    /// <summary>Lets the frontend ask "am I signed in?" without ever reading the token.</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new { id = UserId });

    private void SetAuthCookie(string token)
    {
        var minutes = configuration.GetValue("JwtSettings:ExpiryMinutes", 120);
        Response.Cookies.Append(CookieName, token, CookieOptions(TimeSpan.FromMinutes(minutes)));
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
