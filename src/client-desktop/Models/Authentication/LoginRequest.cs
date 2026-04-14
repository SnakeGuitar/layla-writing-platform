using Layla.Desktop.Models.Validation;
using Layla.Desktop.Services.Validation;

namespace Layla.Desktop.Models.Authentication
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public ValidationResult Validate()
        {
            var result = ValidationResult.Success();

            if (!ValidationService.IsRequired(Email))
                result.AddError(nameof(Email), "Email is required.");
            else if (!ValidationService.IsValidEmail(Email))
                result.AddError(nameof(Email), "Invalid email format.");

            if (!ValidationService.IsRequired(Password))
                result.AddError(nameof(Password), "Password is required.");

            return result;
        }
    }
}