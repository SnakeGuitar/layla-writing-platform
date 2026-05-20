using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Layla.Desktop.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.Services
{
    public class ProjectApiService : IProjectApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProjectApiService> _logger;
        private HubConnection? _presenceHub;

        public event Action<Guid, bool>? AuthorStatusChanged;
        public event Action<Guid, IEnumerable<ParticipantPresence>>? ParticipantsUpdated;
        public event Action<Guid, IEnumerable<VoiceParticipant>>? VoiceParticipantsUpdated;
        public event Action? SessionDisplaced;

        public ProjectApiService(ILogger<ProjectApiService> logger)
        {
            _logger = logger;
            _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.ServerCoreUrl);
        }

        public async Task<IEnumerable<Project>?> GetMyProjectsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/projects");
                if (response.IsSuccessStatusCode)
                {
                    var projects = await response.Content.ReadFromJsonAsync<IEnumerable<Project>>();
                    if (projects != null) return projects;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects");
            }
            return new List<Project>();
        }

        public async Task<Project?> CreateProjectAsync(CreateProjectRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/projects", request);
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<Project>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
            }
            return null;
        }

        public async Task<Project?> UpdateProjectAsync(Guid id, UpdateProjectRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/projects/{id}", request);
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<Project>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", id);
            }
            return null;
        }

        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/projects/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            }
            return false;
        }

        public async Task<IEnumerable<Project>?> GetPublicProjectsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/projects/public");
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<IEnumerable<Project>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public projects");
            }
            return new List<Project>();
        }

        public async Task<Project?> GetProjectByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/projects/{id}");
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<Project>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project {ProjectId}", id);
            }
            return null;
        }

        public async Task<Collaborator?> JoinPublicProjectAsync(Guid projectId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/projects/{projectId}/join", null);
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<Collaborator>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining project {ProjectId}", projectId);
            }
            return null;
        }

        public async Task<Collaborator?> InviteCollaboratorAsync(Guid projectId, InviteCollaboratorRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/api/projects/{projectId}/collaborators", request);
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<Collaborator>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting collaborator to project {ProjectId}", projectId);
            }
            return null;
        }

        public async Task<IEnumerable<Collaborator>?> GetCollaboratorsAsync(Guid projectId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/projects/{projectId}/collaborators");
                if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<IEnumerable<Collaborator>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving collaborators for project {ProjectId}", projectId);
            }
            return new List<Collaborator>();
        }

        public async Task<bool> RemoveCollaboratorAsync(Guid projectId, string collaboratorUserId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/projects/{projectId}/collaborators/{collaboratorUserId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing collaborator {CollaboratorId} from project {ProjectId}", collaboratorUserId, projectId);
            }
            return false;
        }

        public async Task ConnectPresenceHubAsync()
        {
            if (_presenceHub != null) return;

            _presenceHub = new HubConnectionBuilder()
                .WithUrl($"{ConfigurationService.ServerCoreUrl}/hubs/presence", options =>
                {
                    if (SessionManager.IsAuthenticated)
                        options.AccessTokenProvider = () => Task.FromResult<string?>(SessionManager.CurrentToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _presenceHub.On<Guid, bool>("AuthorStatusChanged", (pid, active) => AuthorStatusChanged?.Invoke(pid, active));
            _presenceHub.On<Guid, IEnumerable<ParticipantPresence>>("ParticipantsUpdated", (pid, parts) => {
                ParticipantsUpdated?.Invoke(pid, parts);
            });
            _presenceHub.On("MultipleSessionsDetected", () => {
                SessionDisplaced?.Invoke();
            });

            await _presenceHub.StartAsync();
        }

public async Task WatchProjectAsync(Guid projectId)
        {
            if (_presenceHub?.State == HubConnectionState.Connected)
                await _presenceHub.InvokeAsync("WatchProject", projectId);
        }

        public async Task AuthorHeartbeatAsync(Guid projectId, string role = "Author")
        {
            if (_presenceHub?.State == HubConnectionState.Connected)
                await _presenceHub.InvokeAsync("AuthorHeartbeat", projectId, role);
        }

        public async Task DisconnectPresenceHubAsync()
        {
            if (_presenceHub != null)
            {
                await _presenceHub.StopAsync();
                await _presenceHub.DisposeAsync();
                _presenceHub = null;
            }
        }
    }
}
