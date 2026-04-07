using Layla.Api.Extensions;
using Layla.Core.Common;
using Layla.Core.Constants;
using Layla.Core.Contracts.AppUser;
using Layla.Core.Contracts.Auth;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Layla.Api.Controllers;

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
    /// Get all users (Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
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
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
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
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
    [HttpPost("{id:guid}/ban")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _appUserService.BanAppUserAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return RespondWithError(result.ErrorCode);

        return NoContent();
    }

}