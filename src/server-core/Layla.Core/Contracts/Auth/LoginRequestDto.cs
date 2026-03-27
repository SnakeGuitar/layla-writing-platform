using System.ComponentModel.DataAnnotations;

namespace Layla.Core.Contracts.Auth;

public record LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}