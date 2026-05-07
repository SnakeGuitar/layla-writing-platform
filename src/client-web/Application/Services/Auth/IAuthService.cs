using client_web.Application.Schemas.Auth;
using client_web.Models.Authentication;

namespace client_web.Application.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// Exchanges credentials for a JWT access token.
    /// Returns an <see cref="AuthResult"/> wrapper so callers can branch on
    /// <see cref="AuthResult.IsSuccess"/> without exception handling.
    /// </summary>
    Task<AuthResult> LoginAsync(LoginRequest request);

    /// <summary>
    /// Stage 1 of the two-step register flow.
    /// Creates the user account and triggers a verification email containing
    /// a PIN. The returned <see cref="AuthResult"/> indicates whether the
    /// user record was created — but its <c>Token</c> will be empty, since
    /// the JWT is only issued once the email is verified.
    /// </summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Stage 2 of the two-step register flow.
    /// Submits the PIN that arrived in the verification email. On success the
    /// returned <see cref="AuthResult"/> carries a fully-populated JWT and
    /// the caller can persist it via <c>ISessionManager.SaveAsync</c>.
    /// </summary>
    Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request);
}
