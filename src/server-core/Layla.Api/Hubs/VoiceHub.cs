using Layla.Api.Extensions;
using Layla.Core.Constants;
using Layla.Core.Contracts.Voice;
using Layla.Core.Interfaces;
using Layla.Core.Interfaces.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using VoiceEvents = Layla.Core.Constants.HubConstants.Voice;

namespace Layla.Api.Hubs;

[Authorize]
public class VoiceHub : Hub
{
    private readonly IVoiceRoomManager _roomManager;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<VoiceHub> _logger;

    public VoiceHub(
        IVoiceRoomManager roomManager,
        IProjectRepository projectRepository,
        ILogger<VoiceHub> logger)
    {
        _roomManager = roomManager;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task JoinRoom(Guid projectId)
    {
        var userId = ExtractUserId();
        if (userId == null)
            return;

        var project = await _projectRepository.GetProjectByIdAsync(projectId);
        if (project == null)
            throw new HubException("Project not found.");

        var userRole = await _projectRepository.GetProjectRoleAsync(projectId, userId);

        if (userRole == null && !project.IsPublic)
            throw new HubException("You are not a member of this project.");

        var displayName = Context.User?.GetDisplayName() ?? "Unknown";
        var participantRole = DetermineParticipantRole(userRole?.Role);

        var participant = _roomManager.AddParticipant(projectId, userId, displayName, Context.ConnectionId, participantRole);
        var groupName = HubConstants.GroupNames.VoiceGroup(projectId);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        var participants = _roomManager.GetParticipants(projectId);
        await Clients.Caller.SendAsync(VoiceEvents.RoomState, new VoiceRoomStateDto(projectId, participants));
        await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.UserJoined, participant);

        _logger.LogInformation("User {UserId} joined voice room for project {ProjectId}", userId, projectId);
    }

    private string? ExtractUserId()
    {
        var userId = Context.User!.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("VoiceHub method called but user identity could not be extracted.");
            return null;
        }
        return userId;
    }

    /// <summary>
    /// Determines the participant role for voice communication.
    /// Public members (non-writers) join as READER, writers and above join as their actual role.
    /// </summary>
    private static string DetermineParticipantRole(string? projectRole) =>
        projectRole == null || projectRole == ProjectRoles.Reader ? ProjectRoles.Reader : projectRole;


    public async Task LeaveRoom(Guid projectId)
    {
        var userId = ExtractUserId();
        if (userId == null)
            return;

        var groupName = HubConstants.GroupNames.VoiceGroup(projectId);
        _roomManager.RemoveParticipant(projectId, userId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.UserLeft, userId);

        _logger.LogInformation("User {UserId} left voice room for project {ProjectId}", userId, projectId);
    }

    public async Task StartSpeaking(Guid projectId)
    {
        var userId = ExtractUserId();
        if (userId == null)
            return;

        var participant = _roomManager.GetParticipant(projectId, userId)
            ?? throw new HubException("You are not in this voice room.");

        if (participant.Role == ProjectRoles.Reader)
            throw new HubException("Listeners cannot speak. You have a Reader role in this project.");

        _roomManager.SetSpeaking(projectId, userId, true);

        var groupName = HubConstants.GroupNames.VoiceGroup(projectId);
        await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.UserStartedSpeaking, userId, participant.DisplayName);
    }

    public async Task StopSpeaking(Guid projectId)
    {
        var userId = ExtractUserId();
        if (userId == null)
            return;

        _roomManager.SetSpeaking(projectId, userId, false);

        var groupName = HubConstants.GroupNames.VoiceGroup(projectId);
        await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.UserStoppedSpeaking, userId);
    }

    private const int MaxAudioPayloadBytes = 64 * 1024; // 64 KB

    public async Task SendAudio(Guid projectId, byte[] audioData)
    {
        if (audioData.Length > MaxAudioPayloadBytes)
            throw new HubException($"Audio payload exceeds the maximum allowed size of {MaxAudioPayloadBytes / 1024} KB.");

        var userId = ExtractUserId();
        if (userId == null)
            return;

        var participant = _roomManager.GetParticipant(projectId, userId);

        if (participant == null)
            throw new HubException("You are not in this voice room.");

        if (participant.Role == ProjectRoles.Reader)
            throw new HubException("Listeners cannot send audio. You have a Reader role in this project.");

        var groupName = HubConstants.GroupNames.VoiceGroup(projectId);
        await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.ReceiveAudio, userId, audioData);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _roomManager.RemoveByConnectionId(Context.ConnectionId, out var projectId, out var userId);

        if (projectId.HasValue && userId != null)
        {
            var groupName = GroupName(projectId.Value);
            await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.UserLeft, userId);
            _logger.LogInformation("User {UserId} disconnected from voice room {ProjectId}", userId, projectId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
