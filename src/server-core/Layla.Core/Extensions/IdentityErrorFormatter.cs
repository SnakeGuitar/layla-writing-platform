using Microsoft.AspNetCore.Identity;

namespace Layla.Core.Extensions;

/// <summary>
/// Utility for formatting ASP.NET Core Identity errors into user-friendly strings.
/// Centralizes error message formatting to ensure consistency across the application.
/// </summary>
public static class IdentityErrorFormatter
{
    /// <summary>
    /// Formats a collection of IdentityError objects into a comma-separated string.
    /// </summary>
    /// <param name="errors">The identity errors to format.</param>
    /// <returns>A comma-separated string of error descriptions.</returns>
    public static string Format(IEnumerable<IdentityError> errors) =>
        string.Join(", ", errors.Select(e => e.Description));
}
