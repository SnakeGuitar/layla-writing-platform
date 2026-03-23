using System.Security.Claims;

namespace Layla.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The JWT claim used to carry the user's display name.</summary>
    public const string DisplayNameClaim = "name";

    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value
               ?? user.FindFirstValue("sub")
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>Returns the user's display name from the JWT claim, or <c>null</c> if absent.</summary>
    public static string? GetDisplayName(this ClaimsPrincipal user)
        => user.FindFirst(DisplayNameClaim)?.Value;
}
