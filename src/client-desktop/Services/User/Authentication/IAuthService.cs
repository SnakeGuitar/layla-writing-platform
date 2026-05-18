using Layla.Desktop.Models.User.Authentication;

namespace Layla.Desktop.Services.User.Authentication;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request);
}
