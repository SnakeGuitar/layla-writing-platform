namespace Layla.Core.Constants;

/// <summary>
/// Project role constants. Use these instead of magic strings throughout the codebase.
/// </summary>
public static class ProjectRoles
{
    public const string Owner = "OWNER";
    public const string Editor = "EDITOR";
    public const string Reader = "READER";

    /// <summary>Checks if a role string is a valid project role.</summary>
    public static bool IsValid(string? role) =>
        role == Owner || role == Editor || role == Reader;

    /// <summary>Normalizes a role string to uppercase. Returns the role if valid, null otherwise.</summary>
    public static string? Normalize(string? role) =>
        role?.ToUpperInvariant() switch
        {
            Owner or Editor or Reader => role.ToUpperInvariant(),
            _ => null
        };
}
