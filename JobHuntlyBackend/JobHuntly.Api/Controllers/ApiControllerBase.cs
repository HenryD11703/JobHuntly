using System.Security.Claims;
using JobHuntly.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Authenticated user id from the token; every data query must be scoped by it.</summary>
    protected Guid UserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedException("Invalid token.");
}
