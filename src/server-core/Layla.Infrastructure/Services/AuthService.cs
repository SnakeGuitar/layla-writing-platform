using Layla.Core.Common;
using Layla.Core.Configuration;
using Layla.Core.Constants;
using Layla.Core.Contracts.Auth;
using Layla.Core.Entities;
using Layla.Core.Extensions;
using Layla.Core.Interfaces.Services;
using Layla.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Layla.Infrastructure.Services;

/// <summary>
/// Provides authentication and registration services (login and signup) for users.
///
/// Responsibilities:
/// - Login: Validates email/password, checks account lockout, increments TokenVersion, returns JWT
/// - Register: Creates new user account, assigns Writer role, returns JWT
/// - Token versioning: Each successful login increments the user's TokenVersion claim,
///   invalidating all previous tokens (single-device enforcement)
///
/// Architecture:
/// - Uses ASP.NET Core Identity (UserManager, SignInManager) for user management
/// - Uses ITokenService to generate JWT tokens with embedded TokenVersion
/// - Uses TokenVersionValidator (in middleware) to reject tokens with stale TokenVersion
/// - Inherits from BaseService&lt;AuthService&gt; for centralized exception handling
/// - All public methods return Result&lt;AuthResponseDto&gt; to encapsulate success/failure
///
/// Account Lockout:
/// - After 5 failed password attempts, the account is locked for 15 minutes (ASP.NET Identity default)
/// - Locked accounts receive ErrorCode.AccountLocked (HTTP 423)
/// - Admins can manually ban users via UsersController.BanUser, which also increments TokenVersion
///
/// Token Expiration:
/// - Tokens are valid for 24 hours (configured in JwtSettings:ExpirationInMinutes)
/// - Expired tokens are rejected by TokenVersionValidator during authentication
/// - Each login issues a new token, rendering all previous tokens invalid (via TokenVersion increment)
/// </summary>
/// <param name="userManager">The ASP.NET Core Identity user manager.</param>
/// <param name="signInManager">The ASP.NET Core Identity sign-in manager.</param>
/// <param name="tokenService">Service responsible for generating JWT tokens.</param>
/// <param name="jwtSettings">JWT settings for token expiration and issuer/audience.</param>
/// <param name="logger">Logger for authentication events and errors.</param>
public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings,
    ILogger<AuthService> logger) : BaseService<AuthService>(logger), IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    /// <summary>
    /// Authenticates a user and returns a JWT token if successful.
    /// Overwrites any existing session by incrementing the <see cref="AppUser.TokenVersion"/>.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    /// <returns>A result containing the authentication response with the JWT token.</returns>
    public Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request) =>
        ExecuteAsync(async () =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result<AuthResponseDto>.Failure(ErrorCode.InvalidCredentials);

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return Result<AuthResponseDto>.Failure(ErrorCode.AccountLocked);

            if (!result.Succeeded)
                return Result<AuthResponseDto>.Failure(ErrorCode.InvalidCredentials);

            return await GenerateUserResultAsync(user);
        }, "Failed to login user {Email}", request.Email);

    /// <summary>
    /// Registers a new user and returns a JWT token upon successful creation.
    /// Assigns the default "Writer" role to the newly created user.
    /// </summary>
    /// <param name="request">The registration details.</param>
    /// <returns>A result containing the authentication response with the JWT token.</returns>
    public Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request) =>
        ExecuteAsync(async () =>
        {
            if (await userManager.FindByEmailAsync(request.Email) != null)
                return Result<AuthResponseDto>.Failure(ErrorCode.DuplicateEmail);

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                DisplayName = request.DisplayName ?? request.Email.Split('@')[0],
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = IdentityErrorFormatter.Format(result.Errors);
                return Result<AuthResponseDto>.Failure(ErrorCode.ValidationFailed, $"Registration failed: {errors}");
            }

            await userManager.AddToRoleAsync(user, AppRoles.Writer);

            return await GenerateUserResultAsync(user);
        }, "Failed to register user {Email}", request.Email);

    private async Task<Result<AuthResponseDto>> GenerateUserResultAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        user.TokenVersion++;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = IdentityErrorFormatter.Format(updateResult.Errors);
            return Result<AuthResponseDto>.Failure(ErrorCode.InternalError, $"Failed to update user token version: {errors}");
        }

        var token = tokenService.GenerateToken(user, roles);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            UserId = user.Id,
            Token = token,
            Email = user.Email ?? "",
            DisplayName = user.DisplayName ?? "",
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes)
        });
    }
}
