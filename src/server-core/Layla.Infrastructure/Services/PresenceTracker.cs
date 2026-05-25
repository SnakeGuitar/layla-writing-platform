using Layla.Core.Constants;
using Layla.Core.Contracts;
using Layla.Core.Interfaces;
using System.Collections.Concurrent;

namespace Layla.Infrastructure.Services;

/// <summary>
/// Tracks which users are actively present in which projects via SignalR connections.
/// All public methods are protected by a lock for thread safety; ConcurrentDictionary is used for
/// convenience but the lock is the primary synchronization mechanism.
/// </summary>
public class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<string, (Guid ProjectId, string UserId)> _connections = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, InternalParticipant>> _projectParticipants = new();
    private readonly ConcurrentDictionary<string, List<string>> _userConnections = new();
    private readonly object _lock = new();

    /// <summary>
    /// Heartbeat TTL: a participant is considered stale if no heartbeat has been
    /// received within this window (3 × the 30-second client heartbeat interval).
    /// Stale entries are filtered out from <see cref="GetActiveParticipants"/>.
    /// </summary>
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromSeconds(90);

    private record InternalParticipant(
        string UserId,
        string DisplayName,
        string Role,
        int ConnectionCount,
        string? AvatarUrl,
        DateTimeOffset LastSeen);

    public bool MarkActive(Guid projectId, string userId, string connectionId, string displayName, string role, string? avatarUrl = null)
    {
        lock (_lock)
        {
            _connections[connectionId] = (projectId, userId);
            _userConnections.AddOrUpdate(userId,
                _ => [connectionId],
                (_, list) => { if (!list.Contains(connectionId)) list.Add(connectionId); return list; });

            var participants = _projectParticipants.GetOrAdd(projectId, _ => new ConcurrentDictionary<string, InternalParticipant>());

            bool wasActive = IsProjectActiveUnlocked(projectId);

            var now = DateTimeOffset.UtcNow;
            participants.AddOrUpdate(userId,
                _ => new InternalParticipant(userId, displayName, role, 1, avatarUrl, now),
                (_, existing) => existing with
                {
                    ConnectionCount = existing.ConnectionCount + 1,
                    Role = UpgradeRoleIfNeeded(existing.Role, role),
                    AvatarUrl = avatarUrl ?? existing.AvatarUrl,
                    // Refresh the heartbeat timestamp on every MarkActive call.
                    LastSeen = now
                });

            bool isNowActive = IsProjectActiveUnlocked(projectId);
            return !wasActive && isNowActive;
        }
    }

    private static string UpgradeRoleIfNeeded(string existingRole, string newRole)
    {
        if (!ProjectRoles.IsValid(newRole)) return existingRole;

        if (newRole == ProjectRoles.Owner) return ProjectRoles.Owner;
        if (newRole == ProjectRoles.Editor && existingRole != ProjectRoles.Owner) return ProjectRoles.Editor;
        return existingRole;
    }

    public bool MarkInactive(string connectionId, out Guid projectId, out string userId)
    {
        lock (_lock)
        {
            if (!RemoveConnectionMapping(connectionId, out projectId, out userId))
                return false;

            if (!_projectParticipants.TryGetValue(projectId, out var participants))
                return false;

            bool wasActive = IsProjectActiveUnlocked(projectId);
            DecrementParticipant(participants, userId);

            if (participants.IsEmpty)
                _projectParticipants.TryRemove(projectId, out _);

            bool isNowActive = IsProjectActiveUnlocked(projectId);
            return wasActive && !isNowActive;
        }
    }

    private bool RemoveConnectionMapping(string connectionId, out Guid projectId, out string userId)
    {
        if (!_connections.TryRemove(connectionId, out var info))
        {
            projectId = default;
            userId = string.Empty;
            return false;
        }

        projectId = info.ProjectId;
        userId = info.UserId;

        if (_userConnections.TryGetValue(userId, out var connList))
        {
            connList.Remove(connectionId);
            if (connList.Count == 0)
                _userConnections.TryRemove(userId, out _);
        }

        return true;
    }

    private static void DecrementParticipant(ConcurrentDictionary<string, InternalParticipant> participants, string userId)
    {
        if (!participants.TryGetValue(userId, out var existing))
            return;

        if (existing.ConnectionCount <= 1)
            participants.TryRemove(userId, out _);
        else
            participants[userId] = existing with { ConnectionCount = existing.ConnectionCount - 1 };
    }

    /// <summary>
    /// Checks if a project has any active authors (owner or editor).
    /// Thread-safe — acquires lock internally.
    /// </summary>
    public bool IsProjectActive(Guid projectId)
    {
        lock (_lock)
        {
            return IsProjectActiveUnlocked(projectId);
        }
    }

    /// <summary>
    /// Internal version that assumes the caller already holds the lock.
    /// Use only within lock(\_lock) blocks.
    /// </summary>
    private bool IsProjectActiveUnlocked(Guid projectId)
    {
        if (!_projectParticipants.TryGetValue(projectId, out var participants))
            return false;

        return participants.Values.Any(p =>
            p.Role == ProjectRoles.Owner ||
            p.Role == ProjectRoles.Editor ||
            p.Role == HubConstants.Presence.RoleAuthor);
    }

    public IEnumerable<ParticipantPresenceDto> GetActiveParticipants(Guid projectId)
    {
        lock (_lock)
        {
            if (!_projectParticipants.TryGetValue(projectId, out var participants))
                return Enumerable.Empty<ParticipantPresenceDto>();

            var threshold = DateTimeOffset.UtcNow - StalenessThreshold;
            return participants.Values
                .Where(p => p.LastSeen >= threshold)
                .Select(p => new ParticipantPresenceDto(p.UserId, p.DisplayName, p.Role, p.AvatarUrl))
                .ToList();
        }
    }

    /// <summary>
    /// Returns the most recent active connection ID for the user, or null if none.
    /// Stale IDs (connections that no longer exist) are filtered out automatically.
    /// </summary>
    public string? GetUserConnection(string userId)
    {
        lock (_lock)
        {
            if (!_userConnections.TryGetValue(userId, out var connList))
                return null;

            return connList.LastOrDefault(id => _connections.ContainsKey(id));
        }
    }
}
