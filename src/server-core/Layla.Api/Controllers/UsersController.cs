using Layla.Core.Constants;
using Layla.Core.Contracts.AppUser;
using Layla.Core.Contracts.Auth;
using Layla.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Layla.Api.Controllers;

/// <summary>
/// Manages user accounts: registration, email verification, profile CRUD, and admin actions.
/// All endpoints require a valid JWT Bearer token unless decorated with <see cref="AllowAnonymousAttribute"/>.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAppUserService _appUserService;

    public UsersController(IAuthService authService, IAppUserService appUserService)
    {
        _authService = authService;
        _appUserService = appUserService;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details (display name, email, password).</param>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation error (e.g. weak password, malformed email).</response>
    /// <response code="409">A user with the same email already exists.</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Created(string.Empty, result.Data);
    }

    /// <summary>
    /// Verify user email with a PIN sent during registration.
    /// </summary>
    /// <param name="request">Email address and verification PIN.</param>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="400">Invalid or expired PIN.</response>
    /// <response code="404">No pending verification for this email.</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
    {
        var result = await _authService.VerifyEmailAsync(request.Email, request.Pin);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all users (Admin only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">List of all registered users.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller does not have the Admin role.</response>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var result = await _appUserService.GetAllAppUsersAsync(cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a user by their ID.
    /// </summary>
    /// <remarks>Non-admin users can only retrieve their own profile.</remarks>
    /// <param name="id">User ID (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">User profile.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not the requested user and is not an admin.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);

        if (!isAdmin && (!Guid.TryParse(CurrentUserId, out var currentGuid) || currentGuid != id))
            return Forbid();

        var result = await _appUserService.GetAppUserByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Update the authenticated user's own profile, or an admin can update any user.
    /// </summary>
    /// <param name="id">User ID (GUID).</param>
    /// <param name="request">Updated profile fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Updated user profile.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not the requested user and is not an admin.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateAppUserRequestDto request, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);

        if (!isAdmin && (!Guid.TryParse(CurrentUserId, out var currentGuid) || currentGuid != id))
            return Forbid();

        var result = await _appUserService.UpdateAppUserAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a user account. Admins can delete any user; users can delete only themselves.
    /// </summary>
    /// <param name="id">User ID (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Account deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not the requested user and is not an admin.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);

        if (!isAdmin && (!Guid.TryParse(CurrentUserId, out var currentGuid) || currentGuid != id))
            return Forbid();

        var result = await _appUserService.DeleteAppUserAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return NoContent();
    }

    /// <summary>
    /// Ban a user (Admin only). Invalidates all sessions and locks the account.
    /// </summary>
    /// <remarks>Banning increments the user's <c>TokenVersion</c>, immediately invalidating
    /// all previously issued JWTs across all clients.</remarks>
    /// <param name="id">User ID (GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">User banned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller does not have the Admin role.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("{id:guid}/ban")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _appUserService.BanAppUserAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return NoContent();
    }

}