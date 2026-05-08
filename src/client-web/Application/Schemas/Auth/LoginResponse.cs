namespace client_web.Application.Schemas.Auth;

/// <summary>
/// Mirrors <c>Layla.Core.Contracts.Auth.AuthResponseDto</c> on server-core.
/// Field names must stay in sync with the server contract.
/// </summary>
public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}