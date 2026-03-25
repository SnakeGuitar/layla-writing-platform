using Layla.Core.Common;
using Layla.Core.Configuration;
using Layla.Core.Constants;
using Layla.Core.Contracts.Auth;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Services;
using Layla.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Layla.Infrastructure.Services;

/// <summary>
/// Provides authentication and registration services for users.
/// </summary>
/// <param name="userManager">The ASP.NET Core Identity user manager.</param>
/// <param name="signInManager">The ASP.NET Core Identity sign-in manager.</param>
/// <param name="tokenService">Service responsible for generating JWT tokens.</param>
/// <param name="jwtSettings">JWT settings for token expiration.</param>
/// <param name="logger">Logger for authentication events.</param>
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
                var errors = FormatIdentityErrors(result.Errors);
                return Result<AuthResponseDto>.Failure(ErrorCode.ValidationFailed, $"Registration failed: {errors}");
            }

            await userManager.AddToRoleAsync(user, AppRoles.Writer);

            return await GenerateUserResultAsync(user);
        }, "Failed to register user {Email}", request.Email);

    private static string FormatIdentityErrors(IEnumerable<IdentityError> errors) =>
        string.Join(", ", errors.Select(e => e.Description));

    private async Task<Result<AuthResponseDto>> GenerateUserResultAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        user.TokenVersion++;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = FormatIdentityErrors(updateResult.Errors);
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
