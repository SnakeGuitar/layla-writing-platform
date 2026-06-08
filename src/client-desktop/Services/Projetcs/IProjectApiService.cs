using Layla.Desktop.Models.Projects;

namespace Layla.Desktop.Services.Projetcs;

/// <summary>
/// Desktop adapter for server-core project, collaborator, and presence endpoints.
/// Writing-domain calls (manuscripts, wiki, graph) are handled by the
/// worldbuilding-specific services.
/// </summary>
public interface IProjectApiService
{
    Task<IEnumerable<Project>?> GetMyProjectsAsync();
    Task<IEnumerable<Project>?> GetPublicProjectsAsync();
    Task<Project?> GetProjectByIdAsync(Guid id);
    Task<Project?> CreateProjectAsync(CreateProjectRequest request);
    Task<Project?> UpdateProjectAsync(Guid id, UpdateProjectRequest request);
    Task<bool> DeleteProjectAsync(Guid id);

    Task<Collaborator?> JoinPublicProjectAsync(Guid projectId);
    Task<Collaborator?> InviteCollaboratorAsync(Guid projectId, InviteCollaboratorRequest request);
    Task<IEnumerable<Collaborator>?> GetCollaboratorsAsync(Guid projectId);
    Task<bool> RemoveCollaboratorAsync(Guid projectId, string collaboratorUserId);
    Task<Collaborator?> UpdateCollaboratorRoleAsync(Guid projectId, string collaboratorUserId, string newRole);

    /// <summary>Raised when the presence hub reports whether a project author is active.</summary>
    event Action<Guid, bool>? AuthorStatusChanged;

    /// <summary>Raised with the current project participant snapshot from the presence hub.</summary>
    event Action<Guid, IEnumerable<ParticipantPresence>>? ParticipantsUpdated;

    /// <summary>Raised when server-core rejects the current JWT because another session invalidated it.</summary>
    event Action? SessionDisplaced;

    Task ConnectPresenceHubAsync();
    Task AuthorHeartbeatAsync(Guid projectId, string role = "Author");
    Task WatchProjectAsync(Guid projectId);
    Task DisconnectPresenceHubAsync();
}
