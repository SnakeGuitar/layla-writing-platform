using Layla.Core.Constants;
using Layla.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Layla.Api.Middleware;

/// <summary>
/// Validates JWT token version to detect session invalidation.
/// Fails the token if the user's current TokenVersion doesn't match the token's token_version claim.
/// </summary>
public class TokenVersionValidator
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<TokenVersionValidator> _logger;

    public TokenVersionValidator(UserManager<AppUser> userManager, ILogger<TokenVersionValidator> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal == null)
        {
            context.Fail("No principal.");
            return;
        }

        var userId = ExtractUserId(principal);
        var tokenVersion = ExtractTokenVersion(principal);

        if (string.IsNullOrEmpty(userId) || tokenVersion < 0)
        {
            context.Fail("Invalid token structure (missing user identity or TokenVersion).");
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.TokenVersion != tokenVersion)
        {
            context.Fail("Session expired. User logged in from another device.");
            return;
        }

        // Ensure NameIdentifier claim is present for downstream code
        var identity = principal.Identity as ClaimsIdentity;
        if (identity != null && !principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
    }

    private static string? ExtractUserId(ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimNames.Sub)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private static int ExtractTokenVersion(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirst(ClaimNames.TokenVersion)?.Value, out var version) ? version : -1;
}
