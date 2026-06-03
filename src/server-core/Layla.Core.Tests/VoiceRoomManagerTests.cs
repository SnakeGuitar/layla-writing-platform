using Layla.Core.Constants;
using Layla.Core.Contracts.Voice;
using Layla.Infrastructure.Services;
using Xunit;

namespace Layla.Core.Tests;

// ── AddParticipant — returned DTO ─────────────────────────────────────────────

public class VoiceRoomManager_AddParticipant_ReturnedDto
{
    private readonly VoiceParticipantDto _dto;

    public VoiceRoomManager_AddParticipant_ReturnedDto()
    {
        var sut = new VoiceRoomManager();
        _dto = sut.AddParticipant(Guid.NewGuid(), "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
    }

    [Fact] public void UserId_IsCorrect() => Assert.Equal("u1", _dto.UserId);
    [Fact] public void DisplayName_IsCorrect() => Assert.Equal("Alice", _dto.DisplayName);
    [Fact] public void IsSpeaking_IsFalse() => Assert.False(_dto.IsSpeaking);
    [Fact] public void Role_IsCorrect() => Assert.Equal(HubConstants.Voice.ParticipantRole, _dto.Role);
}

// ── AddParticipant — room is created if it does not exist ────────────────────

public class VoiceRoomManager_AddParticipant_CreatesRoom
{
    private readonly List<VoiceParticipantDto> _participants;

    public VoiceRoomManager_AddParticipant_CreatesRoom()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _participants = sut.GetParticipants(projectId);
    }

    [Fact] public void RoomHasOneParticipant() => Assert.Single(_participants);
}

// ── AddParticipant — state is replaced on rejoin ──────────────────────────────

public class VoiceRoomManager_AddParticipant_ReplacesExistingState
{
    private readonly List<VoiceParticipantDto> _participants;

    public VoiceRoomManager_AddParticipant_ReplacesExistingState()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        sut.AddParticipant(projectId, "u1", "Alice", "conn2", HubConstants.Voice.ParticipantRole);
        _participants = sut.GetParticipants(projectId);
    }

    [Fact] public void OnlyOneEntryExists() => Assert.Single(_participants);
}

// ── RemoveParticipant — project does not exist ───────────────────────────────

public class VoiceRoomManager_RemoveParticipant_WhenProjectNotFound
{
    private readonly bool _result;

    public VoiceRoomManager_RemoveParticipant_WhenProjectNotFound()
    {
        var sut = new VoiceRoomManager();
        _result = sut.RemoveParticipant(Guid.NewGuid(), "u1");
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── RemoveParticipant — user is not in the room ───────────────────────────────

public class VoiceRoomManager_RemoveParticipant_WhenUserNotInRoom
{
    private readonly bool _result;

    public VoiceRoomManager_RemoveParticipant_WhenUserNotInRoom()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _result = sut.RemoveParticipant(projectId, "u-unknown");
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── RemoveParticipant — participant exists ────────────────────────────────────

public class VoiceRoomManager_RemoveParticipant_WhenUserExists
{
    private readonly bool _result;
    private readonly List<VoiceParticipantDto> _remaining;

    public VoiceRoomManager_RemoveParticipant_WhenUserExists()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _result = sut.RemoveParticipant(projectId, "u1");
        _remaining = sut.GetParticipants(projectId);
    }

    [Fact] public void ReturnsTrue() => Assert.True(_result);
    [Fact] public void RoomIsEmpty() => Assert.Empty(_remaining);
}

// ── RemoveByConnectionId — connection is found ────────────────────────────────

public class VoiceRoomManager_RemoveByConnectionId_WhenConnectionFound
{
    private readonly Guid? _outProjectId;
    private readonly string? _outUserId;
    private readonly List<VoiceParticipantDto> _remaining;

    public VoiceRoomManager_RemoveByConnectionId_WhenConnectionFound()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        sut.RemoveByConnectionId("conn1", out _outProjectId, out _outUserId);
        _remaining = sut.GetParticipants(projectId);
    }

    [Fact] public void SetsProjectId() => Assert.NotNull(_outProjectId);
    [Fact] public void SetsUserId() => Assert.Equal("u1", _outUserId);
    [Fact] public void RemovesParticipant() => Assert.Empty(_remaining);
}

// ── RemoveByConnectionId — connection is not found ───────────────────────────

public class VoiceRoomManager_RemoveByConnectionId_WhenConnectionNotFound
{
    private readonly Guid? _outProjectId;
    private readonly string? _outUserId;

    public VoiceRoomManager_RemoveByConnectionId_WhenConnectionNotFound()
    {
        var sut = new VoiceRoomManager();
        sut.RemoveByConnectionId("no-such-conn", out _outProjectId, out _outUserId);
    }

    [Fact] public void ProjectIdIsNull() => Assert.Null(_outProjectId);
    [Fact] public void UserIdIsNull() => Assert.Null(_outUserId);
}

// ── SetSpeaking — project does not exist ─────────────────────────────────────

public class VoiceRoomManager_SetSpeaking_WhenProjectNotFound
{
    private readonly bool _result;

    public VoiceRoomManager_SetSpeaking_WhenProjectNotFound()
    {
        var sut = new VoiceRoomManager();
        _result = sut.SetSpeaking(Guid.NewGuid(), "u1", true);
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── SetSpeaking — user is not in the room ────────────────────────────────────

public class VoiceRoomManager_SetSpeaking_WhenUserNotInRoom
{
    private readonly bool _result;

    public VoiceRoomManager_SetSpeaking_WhenUserNotInRoom()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _result = sut.SetSpeaking(projectId, "u-unknown", true);
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── SetSpeaking — sets IsSpeaking to true ────────────────────────────────────

public class VoiceRoomManager_SetSpeaking_SetsTrue
{
    private readonly bool _setResult;
    private readonly VoiceParticipantDto? _participant;

    public VoiceRoomManager_SetSpeaking_SetsTrue()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _setResult = sut.SetSpeaking(projectId, "u1", true);
        _participant = sut.GetParticipant(projectId, "u1");
    }

    [Fact] public void ReturnsTrue() => Assert.True(_setResult);
    [Fact] public void IsSpeakingIsTrue() => Assert.True(_participant?.IsSpeaking);
}

// ── SetSpeaking — sets IsSpeaking to false ────────────────────────────────────

public class VoiceRoomManager_SetSpeaking_SetsFalse
{
    private readonly VoiceParticipantDto? _participant;

    public VoiceRoomManager_SetSpeaking_SetsFalse()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        sut.SetSpeaking(projectId, "u1", true);
        sut.SetSpeaking(projectId, "u1", false);
        _participant = sut.GetParticipant(projectId, "u1");
    }

    [Fact] public void IsSpeakingIsFalse() => Assert.False(_participant?.IsSpeaking);
}

// ── GetParticipants — project does not exist ──────────────────────────────────

public class VoiceRoomManager_GetParticipants_WhenProjectNotFound
{
    private readonly List<VoiceParticipantDto> _result;

    public VoiceRoomManager_GetParticipants_WhenProjectNotFound()
    {
        var sut = new VoiceRoomManager();
        _result = sut.GetParticipants(Guid.NewGuid());
    }

    [Fact] public void ReturnsEmptyList() => Assert.Empty(_result);
}

// ── GetParticipants — returns correct count ───────────────────────────────────

public class VoiceRoomManager_GetParticipants_WithTwoParticipants
{
    private readonly List<VoiceParticipantDto> _result;

    public VoiceRoomManager_GetParticipants_WithTwoParticipants()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        sut.AddParticipant(projectId, "u2", "Bob", "conn2", HubConstants.Voice.ParticipantRole);
        _result = sut.GetParticipants(projectId);
    }

    [Fact] public void ReturnsTwoParticipants() => Assert.Equal(2, _result.Count);
}

// ── GetParticipant — project does not exist ───────────────────────────────────

public class VoiceRoomManager_GetParticipant_WhenProjectNotFound
{
    private readonly VoiceParticipantDto? _result;

    public VoiceRoomManager_GetParticipant_WhenProjectNotFound()
    {
        var sut = new VoiceRoomManager();
        _result = sut.GetParticipant(Guid.NewGuid(), "u1");
    }

    [Fact] public void ReturnsNull() => Assert.Null(_result);
}

// ── GetParticipant — user does not exist in room ──────────────────────────────

public class VoiceRoomManager_GetParticipant_WhenUserNotFound
{
    private readonly VoiceParticipantDto? _result;

    public VoiceRoomManager_GetParticipant_WhenUserNotFound()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _result = sut.GetParticipant(projectId, "u-unknown");
    }

    [Fact] public void ReturnsNull() => Assert.Null(_result);
}

// ── GetParticipant — participant found ────────────────────────────────────────

public class VoiceRoomManager_GetParticipant_WhenFound
{
    private readonly VoiceParticipantDto? _result;

    public VoiceRoomManager_GetParticipant_WhenFound()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _result = sut.GetParticipant(projectId, "u1");
    }

    [Fact] public void ReturnsCorrectUserId() => Assert.Equal("u1", _result?.UserId);
}

// ── TryConsumeAudioSlot — project does not exist ─────────────────────────────

public class VoiceRoomManager_TryConsumeAudioSlot_WhenProjectNotFound
{
    private readonly bool _result;

    public VoiceRoomManager_TryConsumeAudioSlot_WhenProjectNotFound()
    {
        var sut = new VoiceRoomManager();
        _result = sut.TryConsumeAudioSlot(Guid.NewGuid(), "u1");
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── TryConsumeAudioSlot — user does not exist in room ────────────────────────

public class VoiceRoomManager_TryConsumeAudioSlot_WhenUserNotFound
{
    private readonly bool _result;

    public VoiceRoomManager_TryConsumeAudioSlot_WhenUserNotFound()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _result = sut.TryConsumeAudioSlot(projectId, "u-unknown");
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── TryConsumeAudioSlot — first call succeeds ─────────────────────────────────

public class VoiceRoomManager_TryConsumeAudioSlot_FirstCall
{
    private readonly bool _result;

    public VoiceRoomManager_TryConsumeAudioSlot_FirstCall()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        _result = sut.TryConsumeAudioSlot(projectId, "u1");
    }

    [Fact] public void ReturnsTrue() => Assert.True(_result);
}

// ── TryConsumeAudioSlot — immediate retry is throttled ───────────────────────

public class VoiceRoomManager_TryConsumeAudioSlot_ImmediateRetryIsThrottled
{
    private readonly bool _secondResult;

    public VoiceRoomManager_TryConsumeAudioSlot_ImmediateRetryIsThrottled()
    {
        var sut = new VoiceRoomManager();
        var projectId = Guid.NewGuid();
        sut.AddParticipant(projectId, "u1", "Alice", "conn1", HubConstants.Voice.ParticipantRole);
        sut.TryConsumeAudioSlot(projectId, "u1"); // consume first slot
        _secondResult = sut.TryConsumeAudioSlot(projectId, "u1"); // immediate retry
    }

    [Fact] public void ReturnsFalse() => Assert.False(_secondResult);
}
