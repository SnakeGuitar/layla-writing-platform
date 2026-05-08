using client_web.Application.Schemas.Auth;

namespace client_web.Models.Authentication;

/// <summary>
/// Outcome of an authentication attempt — carries either the decoded
/// <see cref="LoginResponse"/> on success, or a fallback message and per-field
/// validation errors on failure. Mirrors
/// <c>Layla.Desktop.Models.Authentication.AuthResult</c>.
/// </summary>
public class AuthResult
{
    public bool IsSuccess { get; set; }
    public LoginResponse? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, List<string>> ValidationErrors { get; set; } = new();

    public static AuthResult Success(LoginResponse response) => new()
    {
        IsSuccess = true,
        Response = response,
    };

    public static AuthResult Fail(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
    };

    public static AuthResult ValidationError(
        Dictionary<string, List<string>> errors,
        string fallbackMessage = "Validation failed") => new()
    {
        IsSuccess = false,
        ErrorMessage = fallbackMessage,
        ValidationErrors = errors,
    };
}
