using Layla.Api.Extensions;
using Layla.Core.Interfaces.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Layla.Api.Hubs;

[Authorize]
public class ManuscriptHub : Hub
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ManuscriptHub> _logger;

    /// <summary>
    /// Tracks which chapter group each SignalR connection joined so that
    /// <see cref="OnDisconnectedAsync"/> can broadcast the cursor-removed
    /// event without the client needing to call <see cref="LeaveChapterGroup"/>
    /// first (which never happens on abrupt close).
    /// Key = ConnectionId, Value = (projectId, chapterId).
    /// </summary>
    private static readonly ConcurrentDictionary<string, (Guid ProjectId, string ChapterId)>
        _connectionMap = new();

    public ManuscriptHub(IProjectRepository projectRepository, ILogger<ManuscriptHub> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task JoinChapterGroup(Guid projectId, string chapterId)
    {
        var userId = Context.User?.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("JoinChapterGroup called but user identity could not be extracted.");
            return;
        }

        // Validate that user has access to the project
        var hasAccess = await _projectRepository.UserHasAnyRoleInProjectAsync(projectId, userId);
        if (!hasAccess)
        {
            throw new HubException("You do not have access to this project.");
        }

        var groupName = GetChapterGroupName(projectId, chapterId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Track connection → chapter so OnDisconnectedAsync can clean up
        _connectionMap[Context.ConnectionId] = (projectId, chapterId);

        // Also add the user to a unique group based on their UserId so we can target them directly (e.g. for eviction)
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
    }

    /// <summary>
    /// Broadcasts the caller's cursor position to every other member of the
    /// same chapter group. The display name is read from the authenticated
    /// user's JWT claims so clients cannot spoof each other's names.
    /// </summary>
    public async Task SendCursorMoved(Guid projectId, string chapterId, int positionOffset)
    {
        var userId = Context.User?.GetUserId();
        var displayName = Context.User?.GetDisplayName() ?? string.Empty;
        var groupName = GetChapterGroupName(projectId, chapterId);
        await Clients.OthersInGroup(groupName).SendAsync("OnCursorMoved", userId, displayName, positionOffset);
    }

    /// <summary>
    /// Broadcasts the caller's current RTF document content to every other
    /// member of the same chapter group in real time. The receiver applies
    /// the content directly without an API round-trip, so collaborators see
    /// keystrokes with ~350 ms latency instead of the full save-reload cycle.
    /// </summary>
    public async Task BroadcastTextChanged(Guid projectId, string chapterId, string rtfContent)
    {
        var userId    = Context.User?.GetUserId();
        var groupName = GetChapterGroupName(projectId, chapterId);
        await Clients.OthersInGroup(groupName).SendAsync("OnTextChanged", userId, rtfContent);
    }

    /// <summary>
    /// Notifies all other collaborators in the same chapter that the chapter
    /// content has been saved. Receivers reload the chapter from the API so
    /// their view stays in sync without polling.
    /// </summary>
    public async Task NotifyChapterSaved(Guid projectId, string chapterId)
    {
        var groupName = GetChapterGroupName(projectId, chapterId);
        await Clients.OthersInGroup(groupName).SendAsync("OnChapterSaved", projectId, chapterId);
    }

    public async Task LeaveChapterGroup(Guid projectId, string chapterId)
    {
        _connectionMap.TryRemove(Context.ConnectionId, out _);
        var groupName = GetChapterGroupName(projectId, chapterId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Fired by SignalR when a connection closes for any reason — including
    /// abrupt app closure, network drop, or the client calling
    /// <see cref="LeaveChapterGroup"/>. Broadcasts <c>OnCursorRemoved</c> so
    /// collaborators immediately hide the stale cursor marker.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionMap.TryRemove(Context.ConnectionId, out var info))
        {
            var userId = Context.User?.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var groupName = GetChapterGroupName(info.ProjectId, info.ChapterId);
                await Clients.OthersInGroup(groupName).SendAsync("OnCursorRemoved", userId);
                _logger.LogTrace("OnDisconnectedAsync: broadcast OnCursorRemoved for user {UserId} in group {Group}", userId, groupName);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static string GetChapterGroupName(Guid projectId, string chapterId) => $"chapter:{projectId}:{chapterId}";
    private static string GetUserGroupName(string userId) => $"user:{userId}";
}
