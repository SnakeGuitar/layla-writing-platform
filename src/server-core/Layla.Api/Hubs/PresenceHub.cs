using Layla.Api.Extensions;
using Layla.Core.Contracts;
using Layla.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Layla.Api.Hubs;

public class PresenceHub : Hub
{
    private readonly IPresenceTracker _presenceTracker;
    private readonly ILogger<PresenceHub> _logger;

    public PresenceHub(IPresenceTracker presenceTracker, ILogger<PresenceHub> logger)
    {
        _presenceTracker = presenceTracker;
        _logger = logger;
    }

    public async Task WatchProject(Guid projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));
        
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = Context.User?.GetUserId() ?? "Unknown";
            var displayName = Context.User?.GetDisplayName() ?? "Unknown";
            
            var existingConnectionId = _presenceTracker.GetUserConnection(userId);
            if (existingConnectionId != null && existingConnectionId != Context.ConnectionId)
            {
                await Clients.Client(existingConnectionId).SendAsync("MultipleSessionsDetected");
                _logger.LogWarning("User {UserId} logged in from a new instance. Old connection {OldConn} notified.", userId, existingConnectionId);
            }

            _presenceTracker.MarkActive(projectId, userId, Context.ConnectionId, displayName, "Watcher");
            await BroadcastParticipants(projectId);
        }

        var isActive = _presenceTracker.IsProjectActive(projectId);
        await Clients.Caller.SendAsync("AuthorStatusChanged", projectId, isActive);
        
        var participants = _presenceTracker.GetActiveParticipants(projectId);
        await Clients.Caller.SendAsync("ParticipantsUpdated", projectId, participants);
    }

    public async Task UnwatchProject(Guid projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));
    }

    [Authorize]
    public async Task AuthorHeartbeat(Guid projectId, string role = "Author")
    {
        var userId = Context.User!.GetUserId()
            ?? throw new HubException("Invalid user identity.");

        var displayName = Context.User?.GetDisplayName() ?? "Unknown";

        var isFirstAuthor = _presenceTracker.MarkActive(projectId, userId, Context.ConnectionId, displayName, role);

        await BroadcastParticipants(projectId);

        if (isFirstAuthor)
        {
            await Clients.Group(GroupName(projectId)).SendAsync("AuthorStatusChanged", projectId, true);
        }
    }

    private async Task BroadcastParticipants(Guid projectId)
    {
        var participants = _presenceTracker.GetActiveParticipants(projectId);
        await Clients.Group(GroupName(projectId)).SendAsync("ParticipantsUpdated", projectId, participants);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var becameInactive = _presenceTracker.MarkInactive(
            Context.ConnectionId, out var projectId, out var userId);

        if (projectId != default)
        {
            await BroadcastParticipants(projectId);
        }

        if (becameInactive)
        {
            await Clients.Group(GroupName(projectId)).SendAsync("AuthorStatusChanged", projectId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static string GroupName(Guid projectId) => $"presence:{projectId}";
}
