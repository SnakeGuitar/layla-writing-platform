using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Layla.Core.Contracts;
using Layla.Core.Interfaces;

namespace Layla.Infrastructure.Services;

public class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<string, (Guid ProjectId, string UserId)> _connections = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, InternalParticipant>> _projectParticipants = new();
    private readonly ConcurrentDictionary<string, string> _userConnections = new();
    private readonly object _lock = new();

    private record InternalParticipant(string UserId, string DisplayName, string Role, int ConnectionCount);

    public bool MarkActive(Guid projectId, string userId, string connectionId, string displayName, string role)
    {
        _connections[connectionId] = (projectId, userId);
        _userConnections[userId] = connectionId;

        lock (_lock)
        {
            var participants = _projectParticipants.GetOrAdd(projectId, _ => new ConcurrentDictionary<string, InternalParticipant>());
            
            bool wasActive = IsProjectActive(projectId);
            
            participants.AddOrUpdate(userId, 
                _ => new InternalParticipant(userId, displayName, role, 1),
                (_, existing) => {
                    string finalRole = existing.Role;
                    if (role == "Owner" || role == "Author" || role == "Editor") finalRole = role;
                    return existing with { ConnectionCount = existing.ConnectionCount + 1, Role = finalRole };
                });
            
            bool isNowActive = IsProjectActive(projectId);
            return !wasActive && isNowActive;
        }
    }

    public bool MarkInactive(string connectionId, out Guid projectId, out string userId)
    {
        if (!_connections.TryRemove(connectionId, out var info))
        {
            projectId = default;
            userId = string.Empty;
            return false;
        }

        projectId = info.ProjectId;
        userId = info.UserId;

        if (_userConnections.TryGetValue(userId, out var activeConnId) && activeConnId == connectionId)
        {
            _userConnections.TryRemove(userId, out _);
        }

        lock (_lock)
        {
            if (_projectParticipants.TryGetValue(projectId, out var participants))
            {
                bool wasActive = IsProjectActive(projectId);

                if (participants.TryGetValue(userId, out var existing))
                {
                    if (existing.ConnectionCount <= 1)
                    {
                        participants.TryRemove(userId, out _);
                    }
                    else
                    {
                        participants[userId] = existing with { ConnectionCount = existing.ConnectionCount - 1 };
                    }
                }

                if (participants.IsEmpty)
                {
                    _projectParticipants.TryRemove(projectId, out _);
                    return wasActive;
                }

                bool isNowActive = IsProjectActive(projectId);
                return wasActive && !isNowActive;
            }
        }

        return false;
    }

    public bool IsProjectActive(Guid projectId)
    {
        if (!_projectParticipants.TryGetValue(projectId, out var participants))
            return false;

        return participants.Values.Any(p => 
            p.Role == "Owner" || p.Role == "Author" || p.Role == "Editor" || p.Role == "WRITER");
    }

    public IEnumerable<ParticipantPresenceDto> GetActiveParticipants(Guid projectId)
    {
        if (!_projectParticipants.TryGetValue(projectId, out var participants))
            return Enumerable.Empty<ParticipantPresenceDto>();

        return participants.Values.Select(p => new ParticipantPresenceDto(p.UserId, p.DisplayName, p.Role));
    }

    public string? GetUserConnection(string userId)
    {
        return _userConnections.TryGetValue(userId, out var connId) ? connId : null;
    }
}
