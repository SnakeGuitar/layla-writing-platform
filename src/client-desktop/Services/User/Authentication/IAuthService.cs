using Layla.Desktop.Models.User.Authentication;

namespace Layla.Desktop.Services.User.Authentication;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request);

    /// <summary>
    /// Updates the current user's display name, bio and/or avatar URL.
    /// Pass <c>null</c> to leave a field unchanged. Pass <c>""</c> for AvatarUrl to clear it.
    /// On success the session is updated in-memory and persisted to disk.
    /// </summary>
    Task<(bool Success, string? Error)> UpdateProfileAsync(string? displayName, string? bio, string? avatarUrl);
}
