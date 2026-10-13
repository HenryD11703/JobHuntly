using System.ComponentModel.DataAnnotations;

namespace JobHuntly.Application.DTOs;

public class LoginDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
