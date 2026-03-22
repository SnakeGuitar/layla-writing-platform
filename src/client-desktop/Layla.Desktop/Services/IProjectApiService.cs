using System;
using Layla.Desktop.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Layla.Desktop.Services
{
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

        event Action<Guid, bool>? AuthorStatusChanged;
        event Action<Guid, IEnumerable<ParticipantPresence>>? ParticipantsUpdated;
        event Action<Guid, IEnumerable<VoiceParticipant>>? VoiceParticipantsUpdated;
        event Action? SessionDisplaced;

        Task ConnectPresenceHubAsync();
        Task ConnectVoiceHubAsync(Guid projectId);
        Task AuthorHeartbeatAsync(Guid projectId, string role = "Author");
        Task WatchProjectAsync(Guid projectId);
    }
}
