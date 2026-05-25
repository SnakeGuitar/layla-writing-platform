using Layla.Api.Extensions;
using Layla.Core.Constants;
using Layla.Core.Interfaces;
using Layla.Core.Interfaces.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using PresenceEvents = Layla.Core.Constants.HubConstants.Presence;

namespace Layla.Api.Hubs;

public class PresenceHub : Hub
{
    private readonly IPresenceTracker _presenceTracker;
    private readonly IProjectRepository _projectRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly ILogger<PresenceHub> _logger;

    public PresenceHub(
        IPresenceTracker presenceTracker,
        IProjectRepository projectRepository,
        IAppUserRepository appUserRepository,
        ILogger<PresenceHub> logger)
    {
        _presenceTracker = presenceTracker;
        _projectRepository = projectRepository;
        _appUserRepository = appUserRepository;
        _logger = logger;
    }

    /// <summary>Fetches the avatar URL for the given user ID (best-effort; returns null on failure).</summary>
    private async Task<string?> GetAvatarUrlAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid)) return null;
        try
        {
            var result = await _appUserRepository.GetAppUserByIdAsync(userGuid);
            return result.IsSuccess ? result.Data?.AvatarUrl : null;
        }
        catch { return null; }
    }

    [Authorize]
    public async Task WatchProject(Guid projectId)
    {
        var ct = Context.ConnectionAborted;
        await Groups.AddToGroupAsync(Context.ConnectionId, HubConstants.GroupNames.PresenceGroup(projectId), ct);

        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = Context.User?.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("WatchProject called but user identity could not be extracted.");
                return;
            }

            var isMember = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId, ct);
            if (!isMember)
                throw new HubException("You are not a member of this project.");

            var displayName = Context.User?.GetDisplayName() ?? "Unknown";

            var existingConnectionId = _presenceTracker.GetUserConnection(userId);
            if (existingConnectionId != null && existingConnectionId != Context.ConnectionId)
            {
                await Clients.Client(existingConnectionId).SendAsync(PresenceEvents.MultipleSessionsDetected);
                _logger.LogWarning("User {UserId} logged in from a new instance. Old connection {OldConn} notified.", userId, existingConnectionId);
            }

            var avatarUrl = await GetAvatarUrlAsync(userId);
            _presenceTracker.MarkActive(projectId, userId, Context.ConnectionId, displayName, PresenceEvents.RoleWatcher, avatarUrl);
            await BroadcastParticipants(projectId);
        }

        var isActive = _presenceTracker.IsProjectActive(projectId);
        await Clients.Caller.SendAsync(PresenceEvents.AuthorStatusChanged, projectId, isActive);

        var participants = _presenceTracker.GetActiveParticipants(projectId);
        await Clients.Caller.SendAsync(PresenceEvents.ParticipantsUpdated, projectId, participants);
    }

    [Authorize]
    public async Task UnwatchProject(Guid projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubConstants.GroupNames.PresenceGroup(projectId));

        var becameInactive = _presenceTracker.MarkInactive(Context.ConnectionId, out var actualProjectId, out var userId);

        if (actualProjectId != default)
        {
            await BroadcastParticipants(actualProjectId);
        }

        if (becameInactive)
        {
            await Clients.Group(HubConstants.GroupNames.PresenceGroup(actualProjectId)).SendAsync(PresenceEvents.AuthorStatusChanged, actualProjectId, false);
        }
    }

    [Authorize]
    public async Task AuthorHeartbeat(Guid projectId, string role = PresenceEvents.RoleAuthor)
    {
        var userId = Context.User!.GetUserId()
            ?? throw new HubException("Invalid user identity.");

        var displayName = Context.User?.GetDisplayName() ?? "Unknown";
        var avatarUrl = await GetAvatarUrlAsync(userId);

        var isFirstAuthor = _presenceTracker.MarkActive(projectId, userId, Context.ConnectionId, displayName, role, avatarUrl);

        await BroadcastParticipants(projectId);

        if (isFirstAuthor)
        {
            await Clients.Group(HubConstants.GroupNames.PresenceGroup(projectId)).SendAsync(PresenceEvents.AuthorStatusChanged, projectId, true);
        }
    }

    private async Task BroadcastParticipants(Guid projectId)
    {
        var participants = _presenceTracker.GetActiveParticipants(projectId);
        await Clients.Group(HubConstants.GroupNames.PresenceGroup(projectId)).SendAsync(PresenceEvents.ParticipantsUpdated, projectId, participants);
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
            await Clients.Group(HubConstants.GroupNames.PresenceGroup(projectId)).SendAsync(PresenceEvents.AuthorStatusChanged, projectId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

}
