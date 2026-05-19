using Layla.Desktop.Models.User.Validation;
using Layla.Desktop.Services.User.Validation;

namespace Layla.Desktop.Models.User.Authentication;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public ValidationResult Validate()
    {
        ValidationResult? result = ValidationResult.Success();

        if (!ValidationService.IsRequired(this.Email))
            result.AddError(nameof(this.Email), "Email is required.");
        else if (!ValidationService.IsValidEmail(this.Email))
            result.AddError(nameof(this.Email), "Invalid email format.");

        if (!ValidationService.IsRequired(this.Password))
            result.AddError(nameof(this.Password), "Password is required.");

        return result;
    }
}