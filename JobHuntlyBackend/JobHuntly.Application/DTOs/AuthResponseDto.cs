using System.Text.Json.Serialization;

namespace JobHuntly.Application.DTOs;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsDemo { get; set; }

    /// <summary>When a demo sandbox and all its data will be deleted. Null for real accounts.</summary>
    public DateTime? DemoExpiresAt { get; set; }
}

public class AuthResponseDto
{
    /// <summary>
    /// Set by the service so the API layer can put it in an HttpOnly cookie.
    /// Never serialized: the browser must not be able to read the token from JavaScript.
    /// </summary>
    [JsonIgnore]
    public string Token { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
