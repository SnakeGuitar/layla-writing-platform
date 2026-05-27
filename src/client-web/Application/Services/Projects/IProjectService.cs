using client_web.Models;

namespace client_web.Application.Services.Projects;

/// <summary>
/// Project + collaborator endpoints.  Mirrors the desktop client's
/// <c>Layla.Desktop.Services.IProjectApiService</c>: callers no longer pass
/// a JWT, the implementation pulls it from <c>ISessionManager</c>.
/// </summary>
public interface IProjectService
{
    Task<IEnumerable<Project>> GetMyProjectsAsync();
    Task<IEnumerable<Project>> GetAllProjectsAsync();
    Task<List<PublicProjectDto>> GetPublicProjectsAsync();
    Task<Project?> GetProjectByIdAsync(Guid id);
    Task<Project?> CreateProjectAsync(CreateProjectRequest request);
    Task<Project?> UpdateProjectAsync(Guid id, UpdateProjectRequest request);
    Task<bool> DeleteProjectAsync(Guid id);
    Task<bool> JoinPublicProjectAsync(Guid projectId);
    Task<IEnumerable<Collaborator>> GetCollaboratorsAsync(Guid projectId);
    Task<Collaborator?> InviteCollaboratorAsync(Guid projectId, string email, string role);
    Task<Collaborator?> UpdateCollaboratorRoleAsync(Guid projectId, string collaboratorUserId, string role);
    Task<bool> RemoveCollaboratorAsync(Guid projectId, string collaboratorUserId);
}

/// <summary>
/// Mirrors <c>Layla.Core.Contracts.Project.ProjectResponseDto</c> on server-core.
/// </summary>
public record PublicProjectDto(
    Guid Id,
    string Title,
    string Synopsis,
    string LiteraryGenre,
    string? CoverImageUrl,
    DateTime UpdatedAt,
    bool IsPublic
);
