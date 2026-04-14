using System.Collections.Generic;

namespace Layla.Desktop.Models.Authentication
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public AuthResponse? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, List<string>> ValidationErrors { get; set; } = [];

        public static AuthResult Success(AuthResponse response) => new AuthResult
        {
            IsSuccess = true,
            Response = response
        };

        public static AuthResult Fail(string message) => new AuthResult
        {
            IsSuccess = false,
            ErrorMessage = message
        };

        public static AuthResult ValidationError(Dictionary<string, List<string>> errors, string fallbackMessage = "Validation failed") => new AuthResult
        {
            IsSuccess = false,
            ErrorMessage = fallbackMessage,
            ValidationErrors = errors
        };
    }
}
