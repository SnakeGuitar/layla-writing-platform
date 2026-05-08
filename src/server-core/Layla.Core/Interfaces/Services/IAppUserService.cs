using Layla.Core.Common;
using Layla.Core.Contracts.AppUser;

namespace Layla.Core.Interfaces.Services
{
    public interface IAppUserService
    {
        Task<Result<IEnumerable<UserResponseDto>>> GetAllAppUsersAsync(CancellationToken cancellationToken = default);
        Task<Result<UserResponseDto>> GetAppUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Result<UserResponseDto>> UpdateAppUserAsync(Guid userId, UpdateAppUserRequestDto request, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAppUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Result<bool>> BanAppUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}