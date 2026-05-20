using Layla.Api.Extensions;
using Layla.Core.Interfaces.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Layla.Api.Hubs;

[Authorize]
public class ManuscriptHub : Hub
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ManuscriptHub> _logger;

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

        // Also add the user to a unique group based on their UserId so we can target them directly (e.g. for eviction)
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
    }

    public async Task SendCursorMoved(Guid projectId, string chapterId, int positionOffset)
    {
        var userId = Context.User?.GetUserId();
        var groupName = GetChapterGroupName(projectId, chapterId);
        await Clients.OthersInGroup(groupName).SendAsync("OnCursorMoved", userId, positionOffset);
    }

    public async Task LeaveChapterGroup(Guid projectId, string chapterId)
    {
        var groupName = GetChapterGroupName(projectId, chapterId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    private static string GetChapterGroupName(Guid projectId, string chapterId) => $"chapter:{projectId}:{chapterId}";
    private static string GetUserGroupName(string userId) => $"user:{userId}";
}
