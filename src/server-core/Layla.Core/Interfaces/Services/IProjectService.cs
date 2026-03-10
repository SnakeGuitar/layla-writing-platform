using Layla.Core.Contracts.Project;
using Layla.Core.Entities;
using Layla.Core.Common;

namespace Layla.Core.Interfaces.Services;

public interface IProjectService
{
    Task<Result<Project>> CreateProjectAsync(CreateProjectRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Project>>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Project>>> GetUserProjectsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<Project>> UpdateProjectAsync(Guid projectId, UpdateProjectRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
}
