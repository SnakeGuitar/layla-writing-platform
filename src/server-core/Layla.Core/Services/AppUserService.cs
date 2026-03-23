using Layla.Core.Common;
using Layla.Core.Contracts.AppUser;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Layla.Core.Services;

public class AppUserService : IAppUserService
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly ILogger<AppUserService> _logger;

    public AppUserService(IAppUserRepository appUserRepository, ILogger<AppUserService> logger)
    {
        _appUserRepository = appUserRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<UserResponseDto>>> GetAllAppUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _appUserRepository.GetAllAppUsersAsync(cancellationToken);
            if (!result.IsSuccess)
                return Result<IEnumerable<UserResponseDto>>.Failure(result.ErrorCode ?? ErrorCode.InternalError);

            var dtos = result.Data!.Select(MapToResponseDto);
            return Result<IEnumerable<UserResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all users");
            return Result<IEnumerable<UserResponseDto>>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<UserResponseDto>> GetAppUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _appUserRepository.GetAppUserByIdAsync(userId, cancellationToken);
            if (!result.IsSuccess)
                return Result<UserResponseDto>.Failure(ErrorCode.UserNotFound);

            return Result<UserResponseDto>.Success(MapToResponseDto(result.Data!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user {UserId}", userId);
            return Result<UserResponseDto>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<UserResponseDto>> UpdateAppUserAsync(Guid userId, UpdateAppUserRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _appUserRepository.UpdateAppUserAsync(userId, request, cancellationToken);
            if (!result.IsSuccess)
                return Result<UserResponseDto>.Failure(ErrorCode.UserNotFound);

            return Result<UserResponseDto>.Success(MapToResponseDto(result.Data!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            return Result<UserResponseDto>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<bool>> DeleteAppUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _appUserRepository.DeleteAppUserAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return Result<bool>.Failure(ErrorCode.InternalError);
        }
    }

    public async Task<Result<bool>> BanAppUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _appUserRepository.BanAppUserAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ban user {UserId}", userId);
            return Result<bool>.Failure(ErrorCode.InternalError);
        }
    }

    private static UserResponseDto MapToResponseDto(AppUser user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt
        };
    }
}
