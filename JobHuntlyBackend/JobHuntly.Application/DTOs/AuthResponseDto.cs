using System.Text.Json.Serialization;

namespace JobHuntly.Application.DTOs;

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
