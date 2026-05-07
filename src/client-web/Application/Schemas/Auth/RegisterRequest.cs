using System.ComponentModel.DataAnnotations;
using ValidationResult = client_web.Helpers.Validation.ValidationResult;
using ValidationService = client_web.Helpers.Validation.ValidationService;

namespace client_web.Application.Schemas.Auth;

/// <summary>
/// Mirrors <c>Layla.Core.Contracts.Auth.RegisterRequestDto</c> on server-core.
/// </summary>
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Client-side validation pre-check; mirrors the desktop client.</summary>
    public ValidationResult Validate()
    {
        var result = ValidationResult.Success();

        if (!ValidationService.IsRequired(Email))
            result.AddError(nameof(Email), "Email is required.");
        else if (!ValidationService.IsValidEmail(Email))
            result.AddError(nameof(Email), "Invalid email format.");

        if (!ValidationService.IsRequired(Password))
            result.AddError(nameof(Password), "Password is required.");
        else if (!ValidationService.IsStrongPassword(Password))
            result.AddError(nameof(Password),
                "Password must be at least 6 characters, contain a number and an uppercase letter.");

        if (!ValidationService.IsRequired(DisplayName))
            result.AddError(nameof(DisplayName), "Display name is required.");

        return result;
    }
}
