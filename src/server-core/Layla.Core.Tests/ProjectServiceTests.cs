using Layla.Core.Common;
using Layla.Core.Constants;
using Layla.Core.Contracts.Project;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Queue;
using Layla.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Layla.Core.Tests;

file static class ProjectSutFactory
{
    internal record Components(
        IProjectRepository Repo,
        IAppUserRepository UserRepo,
        IEventPublisher Publisher,
        IOutboxRepository Outbox,
        ProjectService Sut);

    internal static Components Create()
    {
        var repo = Substitute.For<IProjectRepository>();
        var userRepo = Substitute.For<IAppUserRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var outbox = Substitute.For<IOutboxRepository>();

        repo.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        return new(repo, userRepo, publisher, outbox,
            new ProjectService(repo, userRepo, publisher, outbox, NullLogger<ProjectService>.Instance));
    }

    internal static readonly string OwnerId = "owner-user-id";
    internal static readonly Guid ProjectId = Guid.NewGuid();
}

// ── CreateProjectAsync — success ──────────────────────────────────────────────

public class ProjectService_CreateProjectAsync_WhenSucceeds
{
    private readonly Result<ProjectResponseDto> _result;

    public ProjectService_CreateProjectAsync_WhenSucceeds()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.AddProjectAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        c.Repo.AddProjectRoleAsync(Arg.Any<ProjectRole>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _result = c.Sut.CreateProjectAsync(
                new CreateProjectRequestDto { Title = "My Novel", IsPublic = true },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void Title_MatchesRequest() => Assert.Equal("My Novel", _result.Data!.Title);
    [Fact] public void IsPublic_MatchesRequest() => Assert.True(_result.Data!.IsPublic);
    [Fact] public void UserRole_IsOwner() => Assert.Equal(ProjectRoles.Owner, _result.Data!.UserRole);
}

// ── CreateProjectAsync — transaction calls ────────────────────────────────────

public class ProjectService_CreateProjectAsync_TransactionBehavior
{
    private readonly IProjectRepository _repo;

    public ProjectService_CreateProjectAsync_TransactionBehavior()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.AddProjectAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        c.Repo.AddProjectRoleAsync(Arg.Any<ProjectRole>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        c.Sut.CreateProjectAsync(
                new CreateProjectRequestDto { Title = "Transactional" },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
        _repo = c.Repo;
    }

    [Fact]
    public void AddProjectAsync_IsCalledOnce() =>
        _repo.Received(1).AddProjectAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());

    [Fact]
    public void AddProjectRoleAsync_IsCalledWithOwnerRole() =>
        _repo.Received(1).AddProjectRoleAsync(
            Arg.Is<ProjectRole>(r => r.Role == ProjectRoles.Owner && r.AppUserId == ProjectSutFactory.OwnerId),
            Arg.Any<CancellationToken>());
}

// ── CreateProjectAsync — database error ──────────────────────────────────────

public class ProjectService_CreateProjectAsync_WhenDatabaseFails
{
    private readonly Result<ProjectResponseDto> _result;

    public ProjectService_CreateProjectAsync_WhenDatabaseFails()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException());
        _result = c.Sut.CreateProjectAsync(
                new CreateProjectRequestDto { Title = "Failing" },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsDatabaseError() => Assert.Equal(ErrorCode.DatabaseError, _result.ErrorCode);
}

// ── UpdateProjectAsync — caller is not owner ──────────────────────────────────

public class ProjectService_UpdateProjectAsync_WhenCallerIsNotOwner
{
    private readonly Result<ProjectResponseDto> _result;

    public ProjectService_UpdateProjectAsync_WhenCallerIsNotOwner()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, "other-user", ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(false);
        _result = c.Sut.UpdateProjectAsync(
                ProjectSutFactory.ProjectId,
                new UpdateProjectRequestDto { Title = "New" },
                "other-user")
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsForbidden() => Assert.Equal(ErrorCode.Forbidden, _result.ErrorCode);
}

// ── UpdateProjectAsync — project not found ────────────────────────────────────

public class ProjectService_UpdateProjectAsync_WhenProjectDoesNotExist
{
    private readonly Result<ProjectResponseDto> _result;

    public ProjectService_UpdateProjectAsync_WhenProjectDoesNotExist()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.Repo.GetProjectByIdAsync(ProjectSutFactory.ProjectId, Arg.Any<CancellationToken>()).Returns((Project?)null);
        _result = c.Sut.UpdateProjectAsync(
                ProjectSutFactory.ProjectId,
                new UpdateProjectRequestDto { Title = "Ghost" },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsProjectNotFound() => Assert.Equal(ErrorCode.ProjectNotFound, _result.ErrorCode);
}

// ── UpdateProjectAsync — owner updates ───────────────────────────────────────

public class ProjectService_UpdateProjectAsync_WhenOwnerUpdates
{
    private readonly Result<ProjectResponseDto> _result;

    public ProjectService_UpdateProjectAsync_WhenOwnerUpdates()
    {
        var c = ProjectSutFactory.Create();
        var project = new Project { Id = ProjectSutFactory.ProjectId, Title = "Old", IsPublic = false };
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.Repo.GetProjectByIdAsync(ProjectSutFactory.ProjectId, Arg.Any<CancellationToken>()).Returns(project);
        c.Repo.UpdateProjectAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        c.Repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _result = c.Sut.UpdateProjectAsync(
                ProjectSutFactory.ProjectId,
                new UpdateProjectRequestDto { Title = "New Title", IsPublic = true },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void Title_IsUpdated() => Assert.Equal("New Title", _result.Data!.Title);
    [Fact] public void IsPublic_IsUpdated() => Assert.True(_result.Data!.IsPublic);
}

// ── DeleteProjectAsync — caller is not owner ─────────────────────────────────

public class ProjectService_DeleteProjectAsync_WhenCallerIsNotOwner
{
    private readonly Result<bool> _result;

    public ProjectService_DeleteProjectAsync_WhenCallerIsNotOwner()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, "editor-id", ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(false);
        _result = c.Sut.DeleteProjectAsync(ProjectSutFactory.ProjectId, "editor-id")
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsForbidden() => Assert.Equal(ErrorCode.Forbidden, _result.ErrorCode);
}

// ── DeleteProjectAsync — owner deletes ───────────────────────────────────────

public class ProjectService_DeleteProjectAsync_WhenOwnerDeletes
{
    private readonly Result<bool> _result;

    public ProjectService_DeleteProjectAsync_WhenOwnerDeletes()
    {
        var c = ProjectSutFactory.Create();
        var project = new Project { Id = ProjectSutFactory.ProjectId, Title = "To Delete" };
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.Repo.GetProjectByIdAsync(ProjectSutFactory.ProjectId, Arg.Any<CancellationToken>()).Returns(project);
        c.Repo.DeleteProjectAsync(project, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        c.Repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _result = c.Sut.DeleteProjectAsync(ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void Data_IsTrue() => Assert.True(_result.Data);
}

// ── GetUserProjectsAsync ──────────────────────────────────────────────────────

public class ProjectService_GetUserProjectsAsync_WhenUserHasProjects
{
    private readonly Result<IEnumerable<ProjectResponseDto>> _result;

    public ProjectService_GetUserProjectsAsync_WhenUserHasProjects()
    {
        var c = ProjectSutFactory.Create();
        var projects = new List<Project>
        {
            new() { Id = Guid.NewGuid(), Title = "Alpha", Roles = [new ProjectRole { AppUserId = ProjectSutFactory.OwnerId, Role = ProjectRoles.Owner }] },
            new() { Id = Guid.NewGuid(), Title = "Beta",  Roles = [new ProjectRole { AppUserId = ProjectSutFactory.OwnerId, Role = ProjectRoles.Editor }] },
        };
        c.Repo.GetProjectsByUserIdAsync(ProjectSutFactory.OwnerId, Arg.Any<CancellationToken>()).Returns(projects);
        _result = c.Sut.GetUserProjectsAsync(ProjectSutFactory.OwnerId).GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void Returns_TwoProjects() => Assert.Equal(2, _result.Data!.Count());
    [Fact] public void AlphaProject_HasOwnerRole() => Assert.Contains(_result.Data!, d => d.Title == "Alpha" && d.UserRole == ProjectRoles.Owner);
    [Fact] public void BetaProject_HasEditorRole() => Assert.Contains(_result.Data!, d => d.Title == "Beta" && d.UserRole == ProjectRoles.Editor);
}

// ── InviteCollaboratorAsync — caller is not owner ─────────────────────────────

public class ProjectService_InviteCollaboratorAsync_WhenCallerIsNotOwner
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_InviteCollaboratorAsync_WhenCallerIsNotOwner()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, "not-owner", ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(false);
        _result = c.Sut.InviteCollaboratorAsync(
                ProjectSutFactory.ProjectId,
                new InviteCollaboratorRequestDto { Email = "invitee@layla.io", Role = ProjectRoles.Editor },
                "not-owner")
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsForbidden() => Assert.Equal(ErrorCode.Forbidden, _result.ErrorCode);
}

// ── InviteCollaboratorAsync — email not found ─────────────────────────────────

public class ProjectService_InviteCollaboratorAsync_WhenEmailNotFound
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_InviteCollaboratorAsync_WhenEmailNotFound()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.UserRepo.GetAppUserByEmailAsync("ghost@layla.io", Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Failure(ErrorCode.UserNotFound));
        _result = c.Sut.InviteCollaboratorAsync(
                ProjectSutFactory.ProjectId,
                new InviteCollaboratorRequestDto { Email = "ghost@layla.io", Role = ProjectRoles.Editor },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsUserNotFound() => Assert.Equal(ErrorCode.UserNotFound, _result.ErrorCode);
}

// ── InviteCollaboratorAsync — already a member ────────────────────────────────

public class ProjectService_InviteCollaboratorAsync_WhenAlreadyMember
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_InviteCollaboratorAsync_WhenAlreadyMember()
    {
        var c = ProjectSutFactory.Create();
        var targetUser = new AppUser { Id = "target-id", Email = "collab@layla.io" };
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.UserRepo.GetAppUserByEmailAsync("collab@layla.io", Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Success(targetUser));
        c.Repo.UserHasAnyRoleInProjectAsync(ProjectSutFactory.ProjectId, "target-id", Arg.Any<CancellationToken>())
            .Returns(true);
        _result = c.Sut.InviteCollaboratorAsync(
                ProjectSutFactory.ProjectId,
                new InviteCollaboratorRequestDto { Email = "collab@layla.io", Role = ProjectRoles.Editor },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsAlreadyMember() => Assert.Equal(ErrorCode.AlreadyMember, _result.ErrorCode);
}

// ── InviteCollaboratorAsync — role is OWNER ───────────────────────────────────

public class ProjectService_InviteCollaboratorAsync_WhenRoleIsOwner
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_InviteCollaboratorAsync_WhenRoleIsOwner()
    {
        var c = ProjectSutFactory.Create();
        var targetUser = new AppUser { Id = "t2", Email = "t2@layla.io" };
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.UserRepo.GetAppUserByEmailAsync("t2@layla.io", Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Success(targetUser));
        c.Repo.UserHasAnyRoleInProjectAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _result = c.Sut.InviteCollaboratorAsync(
                ProjectSutFactory.ProjectId,
                new InviteCollaboratorRequestDto { Email = "t2@layla.io", Role = "OWNER" },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidRole() => Assert.Equal(ErrorCode.InvalidRole, _result.ErrorCode);
}

// ── InviteCollaboratorAsync — role is unknown ─────────────────────────────────

public class ProjectService_InviteCollaboratorAsync_WhenRoleIsUnknown
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_InviteCollaboratorAsync_WhenRoleIsUnknown()
    {
        var c = ProjectSutFactory.Create();
        var targetUser = new AppUser { Id = "t3", Email = "t3@layla.io" };
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.UserRepo.GetAppUserByEmailAsync("t3@layla.io", Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Success(targetUser));
        c.Repo.UserHasAnyRoleInProjectAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _result = c.Sut.InviteCollaboratorAsync(
                ProjectSutFactory.ProjectId,
                new InviteCollaboratorRequestDto { Email = "t3@layla.io", Role = "SUPERUSER" },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidRole() => Assert.Equal(ErrorCode.InvalidRole, _result.ErrorCode);
}

// ── InviteCollaboratorAsync — valid EDITOR invite ────────────────────────────

public class ProjectService_InviteCollaboratorAsync_WhenEditorIsInvited
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_InviteCollaboratorAsync_WhenEditorIsInvited()
    {
        var c = ProjectSutFactory.Create();
        var targetUser = new AppUser { Id = "t4", Email = "editor@layla.io", DisplayName = "Editor" };
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.UserRepo.GetAppUserByEmailAsync("editor@layla.io", Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Success(targetUser));
        c.Repo.UserHasAnyRoleInProjectAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        c.Repo.AddProjectRoleAsync(Arg.Any<ProjectRole>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        c.Repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _result = c.Sut.InviteCollaboratorAsync(
                ProjectSutFactory.ProjectId,
                new InviteCollaboratorRequestDto { Email = "editor@layla.io", Role = "editor" },
                ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void CollaboratorRole_IsEditor() => Assert.Equal(ProjectRoles.Editor, _result.Data!.Role);
    [Fact] public void CollaboratorEmail_MatchesTarget() => Assert.Equal("editor@layla.io", _result.Data!.Email);
}

// ── JoinPublicProjectAsync — project is private ───────────────────────────────

public class ProjectService_JoinPublicProjectAsync_WhenProjectIsPrivate
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_JoinPublicProjectAsync_WhenProjectIsPrivate()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.GetProjectByIdAsync(ProjectSutFactory.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new Project { Id = ProjectSutFactory.ProjectId, IsPublic = false });
        _result = c.Sut.JoinPublicProjectAsync(ProjectSutFactory.ProjectId, Guid.NewGuid().ToString())
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidInput() => Assert.Equal(ErrorCode.InvalidInput, _result.ErrorCode);
}

// ── JoinPublicProjectAsync — project not found ────────────────────────────────

public class ProjectService_JoinPublicProjectAsync_WhenProjectNotFound
{
    private readonly Result<CollaboratorResponseDto> _result;

    public ProjectService_JoinPublicProjectAsync_WhenProjectNotFound()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.GetProjectByIdAsync(ProjectSutFactory.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);
        _result = c.Sut.JoinPublicProjectAsync(ProjectSutFactory.ProjectId, Guid.NewGuid().ToString())
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsProjectNotFound() => Assert.Equal(ErrorCode.ProjectNotFound, _result.ErrorCode);
}

// ── RemoveCollaboratorAsync — trying to remove owner ─────────────────────────

public class ProjectService_RemoveCollaboratorAsync_WhenTryingToRemoveOwner
{
    private readonly Result<bool> _result;

    public ProjectService_RemoveCollaboratorAsync_WhenTryingToRemoveOwner()
    {
        var c = ProjectSutFactory.Create();
        c.Repo.UserHasRoleInProjectAsync(
                ProjectSutFactory.ProjectId, ProjectSutFactory.OwnerId, ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(true);
        c.Repo.GetProjectRoleAsync(ProjectSutFactory.ProjectId, "co-owner", Arg.Any<CancellationToken>())
            .Returns(new ProjectRole { AppUserId = "co-owner", Role = ProjectRoles.Owner });
        _result = c.Sut.RemoveCollaboratorAsync(ProjectSutFactory.ProjectId, "co-owner", ProjectSutFactory.OwnerId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidInput() => Assert.Equal(ErrorCode.InvalidInput, _result.ErrorCode);
}
