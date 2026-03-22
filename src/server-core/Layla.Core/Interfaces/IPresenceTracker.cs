using System;
using System.Collections.Generic;
using Layla.Core.Contracts;

namespace Layla.Core.Interfaces;

public interface IPresenceTracker
{
    bool MarkActive(Guid projectId, string userId, string connectionId, string displayName, string role);
    bool MarkInactive(string connectionId, out Guid projectId, out string userId);
    bool IsProjectActive(Guid projectId);
    IEnumerable<ParticipantPresenceDto> GetActiveParticipants(Guid projectId);
    string? GetUserConnection(string userId);
}
