using Layla.Core.Common;
using Layla.Core.Contracts.Project;

namespace Layla.Core.Interfaces.Services;

public interface IProjectService
{
    Task<Result<ProjectResponseDto>> CreateProjectAsync(CreateProjectRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectResponseDto>>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectResponseDto>>> GetUserProjectsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<ProjectResponseDto>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> UserHasAccessAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task<Result<ProjectResponseDto>> UpdateProjectAsync(Guid projectId, UpdateProjectRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProjectResponseDto>>> GetPublicProjectsAsync(CancellationToken cancellationToken = default);

    Task<Result<CollaboratorResponseDto>> JoinPublicProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task<Result<CollaboratorResponseDto>> InviteCollaboratorAsync(Guid projectId, InviteCollaboratorRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CollaboratorResponseDto>>> GetCollaboratorsAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveCollaboratorAsync(Guid projectId, string collaboratorUserId, string userId, CancellationToken cancellationToken = default);
}
