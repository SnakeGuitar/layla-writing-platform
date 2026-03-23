using Layla.Core.Common;
using Layla.Core.Contracts.AppUser;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Layla.Infrastructure.Data.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private IDbContextTransaction? _currentTransaction;

    public AppUserRepository(ApplicationDbContext dbContext, UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
                await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
                await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
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
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
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
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
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
            var errors = string.Join(", ", lockoutResult.Errors.Select(e => e.Description));
            return Result<bool>.Failure(ErrorCode.ValidationFailed, errors);
        }

        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await _userManager.UpdateAsync(user);

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
