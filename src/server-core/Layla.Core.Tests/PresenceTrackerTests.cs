using Layla.Core.Constants;
using Layla.Core.Contracts;
using Layla.Infrastructure.Services;
using Xunit;

namespace Layla.Core.Tests;

// ── MarkActive — first owner activates project ───────────────────────────────

public class PresenceTracker_MarkActive_WhenFirstOwnerJoins
{
    private readonly bool _result;

    public PresenceTracker_MarkActive_WhenFirstOwnerJoins()
    {
        var sut = new PresenceTracker();
        _result = sut.MarkActive(Guid.NewGuid(), "u1", "conn1", "Alice", ProjectRoles.Owner);
    }

    [Fact] public void ReturnsTrue() => Assert.True(_result);
}

// ── MarkActive — project was already active ───────────────────────────────────

public class PresenceTracker_MarkActive_WhenProjectAlreadyActive
{
    private readonly bool _secondResult;

    public PresenceTracker_MarkActive_WhenProjectAlreadyActive()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        _secondResult = sut.MarkActive(projectId, "u2", "conn2", "Bob", ProjectRoles.Editor);
    }

    [Fact] public void ReturnsFalse() => Assert.False(_secondResult);
}

// ── MarkActive — READER does not activate project ────────────────────────────

public class PresenceTracker_MarkActive_WhenOnlyReaderJoins
{
    private readonly bool _result;

    public PresenceTracker_MarkActive_WhenOnlyReaderJoins()
    {
        var sut = new PresenceTracker();
        _result = sut.MarkActive(Guid.NewGuid(), "u1", "conn1", "Alice", ProjectRoles.Reader);
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── MarkActive — second connection for same user stays one entry ──────────────

public class PresenceTracker_MarkActive_SecondConnectionForSameUser
{
    private readonly IReadOnlyList<ParticipantPresenceDto> _participants;

    public PresenceTracker_MarkActive_SecondConnectionForSameUser()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        sut.MarkActive(projectId, "u1", "conn2", "Alice", ProjectRoles.Owner);
        _participants = sut.GetActiveParticipants(projectId).ToList();
    }

    [Fact] public void SingleParticipantEntry() => Assert.Single(_participants);
}

// ── MarkActive — role upgrade from EDITOR to OWNER ───────────────────────────

public class PresenceTracker_MarkActive_UpgradesRoleToOwner
{
    private readonly ParticipantPresenceDto _participant;

    public PresenceTracker_MarkActive_UpgradesRoleToOwner()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Editor);
        sut.MarkActive(projectId, "u1", "conn2", "Alice", ProjectRoles.Owner);
        _participant = sut.GetActiveParticipants(projectId).First();
    }

    [Fact] public void RoleIsOwner() => Assert.Equal(ProjectRoles.Owner, _participant.Role);
}

// ── MarkActive — OWNER is not downgraded to EDITOR ───────────────────────────

public class PresenceTracker_MarkActive_DoesNotDowngradeOwner
{
    private readonly ParticipantPresenceDto _participant;

    public PresenceTracker_MarkActive_DoesNotDowngradeOwner()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        sut.MarkActive(projectId, "u1", "conn2", "Alice", ProjectRoles.Editor);
        _participant = sut.GetActiveParticipants(projectId).First();
    }

    [Fact] public void RoleRemainsOwner() => Assert.Equal(ProjectRoles.Owner, _participant.Role);
}

// ── MarkActive — existing avatarUrl preserved when null is passed ─────────────

public class PresenceTracker_MarkActive_PreservesAvatarWhenNullPassed
{
    private readonly ParticipantPresenceDto _participant;

    public PresenceTracker_MarkActive_PreservesAvatarWhenNullPassed()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner, "https://original.png");
        sut.MarkActive(projectId, "u1", "conn2", "Alice", ProjectRoles.Owner, null);
        _participant = sut.GetActiveParticipants(projectId).First();
    }

    [Fact] public void AvatarUrlIsPreserved() => Assert.Equal("https://original.png", _participant.AvatarUrl);
}

// ── MarkActive — avatarUrl updated when a new value is provided ───────────────

public class PresenceTracker_MarkActive_UpdatesAvatarUrl
{
    private readonly ParticipantPresenceDto _participant;

    public PresenceTracker_MarkActive_UpdatesAvatarUrl()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner, "https://old.png");
        sut.MarkActive(projectId, "u1", "conn2", "Alice", ProjectRoles.Owner, "https://new.png");
        _participant = sut.GetActiveParticipants(projectId).First();
    }

    [Fact] public void AvatarUrlIsUpdated() => Assert.Equal("https://new.png", _participant.AvatarUrl);
}

// ── MarkInactive — unknown connectionId ──────────────────────────────────────

public class PresenceTracker_MarkInactive_WhenConnectionUnknown
{
    private readonly bool _result;

    public PresenceTracker_MarkInactive_WhenConnectionUnknown()
    {
        var sut = new PresenceTracker();
        _result = sut.MarkInactive("unknown-conn", out _, out _);
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── MarkInactive — last participant leaves ───────────────────────────────────

public class PresenceTracker_MarkInactive_WhenLastParticipantLeaves
{
    private readonly bool _result;
    private readonly Guid _outProjectId;
    private readonly string _outUserId;

    public PresenceTracker_MarkInactive_WhenLastParticipantLeaves()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        _result = sut.MarkInactive("conn1", out _outProjectId, out _outUserId);
    }

    [Fact] public void ReturnsTrue() => Assert.True(_result);
    [Fact] public void SetsProjectId() => Assert.NotEqual(Guid.Empty, _outProjectId);
    [Fact] public void SetsUserId() => Assert.Equal("u1", _outUserId);
}

// ── MarkInactive — other participants remain, project stays active ─────────────

public class PresenceTracker_MarkInactive_WhenOtherParticipantsRemain
{
    private readonly bool _result;

    public PresenceTracker_MarkInactive_WhenOtherParticipantsRemain()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        sut.MarkActive(projectId, "u2", "conn2", "Bob", ProjectRoles.Editor);
        _result = sut.MarkInactive("conn1", out _, out _);
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── MarkInactive — removing one of two connections keeps participant ───────────

public class PresenceTracker_MarkInactive_WithTwoConnectionsForSameUser
{
    private readonly IReadOnlyList<ParticipantPresenceDto> _participants;

    public PresenceTracker_MarkInactive_WithTwoConnectionsForSameUser()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        sut.MarkActive(projectId, "u1", "conn2", "Alice", ProjectRoles.Owner);
        sut.MarkInactive("conn1", out _, out _);
        _participants = sut.GetActiveParticipants(projectId).ToList();
    }

    [Fact] public void ParticipantStillPresent() => Assert.Single(_participants);
}

// ── IsProjectActive — project does not exist ─────────────────────────────────

public class PresenceTracker_IsProjectActive_WhenProjectDoesNotExist
{
    private readonly bool _result;

    public PresenceTracker_IsProjectActive_WhenProjectDoesNotExist()
    {
        var sut = new PresenceTracker();
        _result = sut.IsProjectActive(Guid.NewGuid());
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── IsProjectActive — only READER present ─────────────────────────────────────

public class PresenceTracker_IsProjectActive_WhenOnlyReaderPresent
{
    private readonly bool _result;

    public PresenceTracker_IsProjectActive_WhenOnlyReaderPresent()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Reader);
        _result = sut.IsProjectActive(projectId);
    }

    [Fact] public void ReturnsFalse() => Assert.False(_result);
}

// ── IsProjectActive — EDITOR present ──────────────────────────────────────────

public class PresenceTracker_IsProjectActive_WhenEditorPresent
{
    private readonly bool _result;

    public PresenceTracker_IsProjectActive_WhenEditorPresent()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Editor);
        _result = sut.IsProjectActive(projectId);
    }

    [Fact] public void ReturnsTrue() => Assert.True(_result);
}

// ── IsProjectActive — presence-hub "Author" role present ──────────────────────

public class PresenceTracker_IsProjectActive_WhenAuthorRolePresent
{
    private readonly bool _result;

    public PresenceTracker_IsProjectActive_WhenAuthorRolePresent()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", HubConstants.Presence.RoleAuthor);
        _result = sut.IsProjectActive(projectId);
    }

    [Fact] public void ReturnsTrue() => Assert.True(_result);
}

// ── GetActiveParticipants — project not found ─────────────────────────────────

public class PresenceTracker_GetActiveParticipants_WhenProjectNotFound
{
    private readonly IEnumerable<ParticipantPresenceDto> _result;

    public PresenceTracker_GetActiveParticipants_WhenProjectNotFound()
    {
        var sut = new PresenceTracker();
        _result = sut.GetActiveParticipants(Guid.NewGuid());
    }

    [Fact] public void ReturnsEmpty() => Assert.Empty(_result);
}

// ── GetActiveParticipants — DTO fields are mapped correctly ───────────────────

public class PresenceTracker_GetActiveParticipants_WithOneParticipant
{
    private readonly IReadOnlyList<ParticipantPresenceDto> _result;

    public PresenceTracker_GetActiveParticipants_WithOneParticipant()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Editor, "https://avatar.png");
        _result = sut.GetActiveParticipants(projectId).ToList();
    }

    [Fact] public void ReturnsOneEntry() => Assert.Single(_result);
    [Fact] public void UserId_IsCorrect() => Assert.Equal("u1", _result[0].UserId);
    [Fact] public void DisplayName_IsCorrect() => Assert.Equal("Alice", _result[0].DisplayName);
    [Fact] public void Role_IsCorrect() => Assert.Equal(ProjectRoles.Editor, _result[0].Role);
    [Fact] public void AvatarUrl_IsCorrect() => Assert.Equal("https://avatar.png", _result[0].AvatarUrl);
}

// ── GetUserConnection — user not registered ────────────────────────────────────

public class PresenceTracker_GetUserConnection_WhenUserUnknown
{
    private readonly string? _result;

    public PresenceTracker_GetUserConnection_WhenUserUnknown()
    {
        var sut = new PresenceTracker();
        _result = sut.GetUserConnection("nobody");
    }

    [Fact] public void ReturnsNull() => Assert.Null(_result);
}

// ── GetUserConnection — user has an active connection ─────────────────────────

public class PresenceTracker_GetUserConnection_WhenUserConnected
{
    private readonly string? _result;

    public PresenceTracker_GetUserConnection_WhenUserConnected()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        _result = sut.GetUserConnection("u1");
    }

    [Fact] public void ReturnsConnectionId() => Assert.Equal("conn1", _result);
}

// ── GetUserConnection — after disconnect ──────────────────────────────────────

public class PresenceTracker_GetUserConnection_AfterDisconnect
{
    private readonly string? _result;

    public PresenceTracker_GetUserConnection_AfterDisconnect()
    {
        var sut = new PresenceTracker();
        var projectId = Guid.NewGuid();
        sut.MarkActive(projectId, "u1", "conn1", "Alice", ProjectRoles.Owner);
        sut.MarkInactive("conn1", out _, out _);
        _result = sut.GetUserConnection("u1");
    }

    [Fact] public void ReturnsNull() => Assert.Null(_result);
}
