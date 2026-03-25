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
        var userRole = await _projectRepository.GetProjectRoleAsync(projectId, userId);

        if (userRole == null && (project == null || !project.IsPublic))
            throw new HubException("You are not a member of this project.");

        var displayName = Context.User?.GetDisplayName() ?? "Unknown";
        var participantRole = DetermineParticipantRole(userRole?.Role);

        var participant = _roomManager.AddParticipant(projectId, userId, displayName, Context.ConnectionId, participantRole);
        var groupName = GroupName(projectId);

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

    private static string DetermineParticipantRole(string? projectRole) =>
        projectRole == ProjectRoles.Reader || projectRole == null ? ProjectRoles.Reader : VoiceEvents.ParticipantRole;

    private static string GroupName(Guid projectId) => $"voice:{projectId}";

    public async Task LeaveRoom(Guid projectId)
    {
        var userId = ExtractUserId();
        if (userId == null)
            return;

        var groupName = GroupName(projectId);
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

        var groupName = GroupName(projectId);
        await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.UserStartedSpeaking, userId, participant.DisplayName);
    }

    public async Task StopSpeaking(Guid projectId)
    {
        var userId = ExtractUserId();
        if (userId == null)
            return;

        _roomManager.SetSpeaking(projectId, userId, false);

        var groupName = GroupName(projectId);
        await Clients.OthersInGroup(groupName).SendAsync(VoiceEvents.UserStoppedSpeaking, userId);
    }

    public async Task SendAudio(Guid projectId, byte[] audioData)
    {
        var userId = ExtractUserId();
        if (userId == null)
            return;

        var participant = _roomManager.GetParticipant(projectId, userId);

        if (participant == null || participant.Role == ProjectRoles.Reader)
            return; // Silently drop audio from non-members or listeners

        var groupName = GroupName(projectId);
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
