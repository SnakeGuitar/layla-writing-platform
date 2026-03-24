using Layla.Core.Entities;

namespace Layla.Core.Interfaces.Data;

/// <summary>
/// Repository for project and project role management with transaction support.
/// </summary>
public interface IProjectRepository : ITransactionalRepository
{
    Task AddProjectAsync(Project project, CancellationToken cancellationToken = default);
    Task AddProjectRoleAsync(ProjectRole projectRole, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetProjectsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetAllProjectsAsync(CancellationToken cancellationToken = default);

    Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task UpdateProjectAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Project project, CancellationToken cancellationToken = default);
    Task<bool> UserHasRoleInProjectAsync(Guid projectId, string userId, string role, CancellationToken cancellationToken = default);
    Task<bool> UserHasAnyRoleInProjectAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetPublicProjectsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ProjectRole>> GetProjectCollaboratorsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectRole?> GetProjectRoleAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task RemoveProjectRoleAsync(ProjectRole role, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
