using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Layla.Core.Contracts.Voice;
using Layla.Core.Interfaces;

namespace Layla.Infrastructure.Services;

public class VoiceRoomManager : IVoiceRoomManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ParticipantState>> _rooms = new();

    private record ParticipantState(
        string UserId,
        string DisplayName,
        string ConnectionId,
        string Role,
        DateTime JoinedAt,
        bool IsSpeaking
    );

    public VoiceParticipantDto AddParticipant(Guid projectId, string userId, string displayName, string connectionId, string role)
    {
        var room = _rooms.GetOrAdd(projectId, _ => new ConcurrentDictionary<string, ParticipantState>());
        var state = new ParticipantState(userId, displayName, connectionId, role, DateTime.UtcNow, false);
        room.AddOrUpdate(userId, state, (_, _) => state);

        return ToDto(state);
    }

    public bool RemoveParticipant(Guid projectId, string userId)
    {
        if (!_rooms.TryGetValue(projectId, out var room))
            return false;

        var removed = room.TryRemove(userId, out _);

        if (room.IsEmpty)
            _rooms.TryRemove(projectId, out _);

        return removed;
    }

    public void RemoveByConnectionId(string connectionId, out Guid? projectId, out string? userId)
    {
        projectId = null;
        userId = null;

        foreach (var (pid, room) in _rooms)
        {
            foreach (var (uid, state) in room)
            {
                if (state.ConnectionId == connectionId)
                {
                    projectId = pid;
                    userId = uid;
                    room.TryRemove(uid, out _);

                    if (room.IsEmpty)
                        _rooms.TryRemove(pid, out _);

                    return;
                }
            }
        }
    }

    public bool SetSpeaking(Guid projectId, string userId, bool isSpeaking)
    {
        if (!_rooms.TryGetValue(projectId, out var room))
            return false;

        if (!room.TryGetValue(userId, out var state))
            return false;

        room[userId] = state with { IsSpeaking = isSpeaking };
        return true;
    }

    public List<VoiceParticipantDto> GetParticipants(Guid projectId)
    {
        if (!_rooms.TryGetValue(projectId, out var room))
            return [];

        return room.Values.Select(ToDto).ToList();
    }

    public VoiceParticipantDto? GetParticipant(Guid projectId, string userId)
    {
        if (!_rooms.TryGetValue(projectId, out var room))
            return null;

        return room.TryGetValue(userId, out var state) ? ToDto(state) : null;
    }

    private static VoiceParticipantDto ToDto(ParticipantState s) =>
        new(s.UserId, s.DisplayName, s.IsSpeaking, s.Role, s.JoinedAt);
}
