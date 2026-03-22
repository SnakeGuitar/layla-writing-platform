using Layla.Api.Extensions;
using Layla.Core.Contracts.Voice;
using Layla.Core.Interfaces.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
        var userId = Context.User!.GetUserId()
            ?? throw new HubException("Invalid user identity.");

        var hasAccess = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId);
        var project = await _projectRepository.GetProjectByIdAsync(projectId);
        
        if (!hasAccess && (project == null || !project.IsPublic))
            throw new HubException("You are not a member of this project.");

        var displayName = Context.User?.FindFirst("name")?.Value ?? "Unknown";

        // Determine role for speaker/listener distinction
        string role;
        if (hasAccess)
        {
            var isReader = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, "Reader");
            role = isReader ? "Reader" : "Speaker";
        }
        else
        {
            // Public project watcher
            role = "Reader";
        }

        var participant = _roomManager.AddParticipant(projectId, userId, displayName, Context.ConnectionId, role);
        var groupName = $"voice:{projectId}";

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Send current room state to the joining user
        var participants = _roomManager.GetParticipants(projectId);
        await Clients.Caller.SendAsync("RoomState", new VoiceRoomStateDto(projectId, participants));

        // Notify others
        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", participant);

        _logger.LogInformation("User {UserId} joined voice room for project {ProjectId}", userId, projectId);
    }

    public async Task LeaveRoom(Guid projectId)
    {
        var userId = Context.User!.GetUserId()
            ?? throw new HubException("Invalid user identity.");

        var groupName = $"voice:{projectId}";
        _roomManager.RemoveParticipant(projectId, userId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.OthersInGroup(groupName).SendAsync("UserLeft", userId);

        _logger.LogInformation("User {UserId} left voice room for project {ProjectId}", userId, projectId);
    }

    public async Task StartSpeaking(Guid projectId)
    {
        var userId = Context.User!.GetUserId()
            ?? throw new HubException("Invalid user identity.");

        var participant = _roomManager.GetParticipant(projectId, userId)
            ?? throw new HubException("You are not in this voice room.");

        if (participant.Role == "Reader")
            throw new HubException("Listeners cannot speak. You have a Reader role in this project.");

        _roomManager.SetSpeaking(projectId, userId, true);

        var groupName = $"voice:{projectId}";
        await Clients.OthersInGroup(groupName).SendAsync("UserStartedSpeaking", userId, participant.DisplayName);
    }

    public async Task StopSpeaking(Guid projectId)
    {
        var userId = Context.User!.GetUserId()
            ?? throw new HubException("Invalid user identity.");

        _roomManager.SetSpeaking(projectId, userId, false);

        var groupName = $"voice:{projectId}";
        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedSpeaking", userId);
    }

    public async Task SendAudio(Guid projectId, byte[] audioData)
    {
        var userId = Context.User!.GetUserId();
        var participant = _roomManager.GetParticipant(projectId, userId!);

        if (participant == null || participant.Role == "Reader")
            return; // Silently drop audio from non-members or listeners

        var groupName = $"voice:{projectId}";
        await Clients.OthersInGroup(groupName).SendAsync("ReceiveAudio", userId, audioData);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _roomManager.RemoveByConnectionId(Context.ConnectionId, out var projectId, out var userId);

        if (projectId.HasValue && userId != null)
        {
            var groupName = $"voice:{projectId.Value}";
            await Clients.OthersInGroup(groupName).SendAsync("UserLeft", userId);
            _logger.LogInformation("User {UserId} disconnected from voice room {ProjectId}", userId, projectId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
