using Layla.Core.Contracts.Voice;

namespace Layla.Core.Interfaces;

public interface IVoiceRoomManager
{
    VoiceParticipantDto AddParticipant(Guid projectId, string userId, string displayName, string connectionId, string role);
    bool RemoveParticipant(Guid projectId, string userId);
    void RemoveByConnectionId(string connectionId, out Guid? projectId, out string? userId);
    bool SetSpeaking(Guid projectId, string userId, bool isSpeaking);
    List<VoiceParticipantDto> GetParticipants(Guid projectId);
    VoiceParticipantDto? GetParticipant(Guid projectId, string userId);
    /// <summary>
    /// Returns true and updates the last-sent timestamp if the user is allowed to send audio.
    /// Returns false if the user is sending too fast (throttled).
    /// </summary>
    bool TryConsumeAudioSlot(Guid projectId, string userId);
}
