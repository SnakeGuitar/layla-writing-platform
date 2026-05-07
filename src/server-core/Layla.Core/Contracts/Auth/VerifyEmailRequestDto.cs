namespace Layla.Core.Contracts.Auth;

public class VerifyEmailRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}
