using Layla.Core.Common;
using Layla.Core.Contracts.AppUser;
using Layla.Core.Entities;
using Layla.Core.Extensions;
using Layla.Core.Interfaces.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Layla.Infrastructure.Data.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly UserManager<AppUser> _userManager;

    public AppUserRepository(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<IEnumerable<AppUser>>> GetAllAppUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<AppUser>>.Success(users);
    }

    public async Task<Result<AppUser>> GetAppUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<AppUser>.Failure(ErrorCode.UserNotFound);

        return Result<AppUser>.Success(user);
    }

    public async Task<Result<AppUser>> UpdateAppUserAsync(Guid userId, UpdateAppUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<AppUser>.Failure(ErrorCode.UserNotFound);

        user.DisplayName = request.DisplayName ?? user.DisplayName;
        user.Bio = request.Bio ?? user.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = IdentityErrorFormatter.Format(result.Errors);
            return Result<AppUser>.Failure(ErrorCode.ValidationFailed, errors);
        }

        return Result<AppUser>.Success(user);
    }

    public async Task<Result<bool>> DeleteAppUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<bool>.Failure(ErrorCode.UserNotFound);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = IdentityErrorFormatter.Format(result.Errors);
            return Result<bool>.Failure(ErrorCode.ValidationFailed, errors);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> BanAppUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<bool>.Failure(ErrorCode.UserNotFound);

        user.TokenVersion++;

        var lockoutResult = await _userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutResult.Succeeded)
        {
            var errors = IdentityErrorFormatter.Format(lockoutResult.Errors);
            return Result<bool>.Failure(ErrorCode.ValidationFailed, errors);
        }

        var lockoutEndResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!lockoutEndResult.Succeeded)
        {
            var errors = IdentityErrorFormatter.Format(lockoutEndResult.Errors);
            return Result<bool>.Failure(ErrorCode.ValidationFailed, errors);
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = IdentityErrorFormatter.Format(updateResult.Errors);
            return Result<bool>.Failure(ErrorCode.ValidationFailed, errors);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<AppUser>> GetAppUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<AppUser>.Failure(ErrorCode.UserNotFound);

        return Result<AppUser>.Success(user);
    }
}
