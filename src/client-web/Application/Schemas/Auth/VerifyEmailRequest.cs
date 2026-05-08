namespace client_web.Application.Schemas.Auth;

/// <summary>
/// Mirrors <c>Layla.Core.Contracts.Auth.VerifyEmailRequestDto</c> on server-core.
/// Two-step register flow: <c>POST /api/users</c> creates the user and emails a
/// PIN; the client then submits the PIN to <c>POST /api/users/verify-email</c>
/// to receive the actual JWT.
/// </summary>
public class VerifyEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}
