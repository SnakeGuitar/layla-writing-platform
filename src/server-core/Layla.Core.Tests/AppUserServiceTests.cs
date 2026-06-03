using Layla.Core.Common;
using Layla.Core.Contracts.AppUser;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Layla.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Layla.Core.Tests;

// ── helpers ───────────────────────────────────────────────────────────────────

file static class AppUserSutFactory
{
    internal static (AppUserService Sut, IAppUserRepository Repo) Create()
    {
        var repo = Substitute.For<IAppUserRepository>();
        return (new AppUserService(repo, NullLogger<AppUserService>.Instance), repo);
    }

    internal static AppUser MakeUser(Guid? id = null) => new()
    {
        Id = (id ?? Guid.NewGuid()).ToString(),
        UserName = "alice",
        Email = "alice@layla.io",
        DisplayName = "Alice",
        Bio = "Author bio",
        AvatarUrl = "https://avatar.png",
        CreatedAt = DateTime.UtcNow,
    };
}

// ── GetAllAppUsersAsync — repo returns users ───────────────────────────────────

public class AppUserService_GetAllAppUsersAsync_WhenUsersExist
{
    private readonly Result<IEnumerable<UserResponseDto>> _result;

    public AppUserService_GetAllAppUsersAsync_WhenUsersExist()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        var users = new[] { AppUserSutFactory.MakeUser(), AppUserSutFactory.MakeUser() };
        repo.GetAllAppUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<AppUser>>.Success(users));
        _result = sut.GetAllAppUsersAsync().GetAwaiter().GetResult();
    }

    [Fact] public void IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void ReturnsOneDtoPerUser() => Assert.Equal(2, _result.Data!.Count());
}

// ── GetAllAppUsersAsync — repo fails ──────────────────────────────────────────

public class AppUserService_GetAllAppUsersAsync_WhenRepoFails
{
    private readonly Result<IEnumerable<UserResponseDto>> _result;

    public AppUserService_GetAllAppUsersAsync_WhenRepoFails()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        repo.GetAllAppUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<AppUser>>.Failure(ErrorCode.DatabaseError));
        _result = sut.GetAllAppUsersAsync().GetAwaiter().GetResult();
    }

    [Fact] public void IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCodeForwarded() => Assert.Equal(ErrorCode.DatabaseError, _result.ErrorCode);
}

// ── GetAppUserByIdAsync — user found ──────────────────────────────────────────

public class AppUserService_GetAppUserByIdAsync_WhenFound
{
    private readonly Result<UserResponseDto> _result;

    public AppUserService_GetAppUserByIdAsync_WhenFound()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        var userId = Guid.NewGuid();
        repo.GetAppUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Success(AppUserSutFactory.MakeUser(userId)));
        _result = sut.GetAppUserByIdAsync(userId).GetAwaiter().GetResult();
    }

    [Fact] public void IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void EmailIsMapped() => Assert.Equal("alice@layla.io", _result.Data!.Email);
    [Fact] public void DisplayNameIsMapped() => Assert.Equal("Alice", _result.Data!.DisplayName);
}

// ── GetAppUserByIdAsync — user not found ──────────────────────────────────────

public class AppUserService_GetAppUserByIdAsync_WhenNotFound
{
    private readonly Result<UserResponseDto> _result;

    public AppUserService_GetAppUserByIdAsync_WhenNotFound()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        repo.GetAppUserByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Failure(ErrorCode.UserNotFound));
        _result = sut.GetAppUserByIdAsync(Guid.NewGuid()).GetAwaiter().GetResult();
    }

    [Fact] public void IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCodeIsUserNotFound() => Assert.Equal(ErrorCode.UserNotFound, _result.ErrorCode);
}

// ── UpdateAppUserAsync — update succeeds ──────────────────────────────────────

public class AppUserService_UpdateAppUserAsync_WhenSuccess
{
    private readonly Result<UserResponseDto> _result;

    public AppUserService_UpdateAppUserAsync_WhenSuccess()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        var userId = Guid.NewGuid();
        var updated = AppUserSutFactory.MakeUser(userId);
        updated.DisplayName = "Alice Updated";
        repo.UpdateAppUserAsync(userId, Arg.Any<UpdateAppUserRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Success(updated));
        _result = sut.UpdateAppUserAsync(userId, new UpdateAppUserRequestDto { DisplayName = "Alice Updated" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void DisplayNameReflectsUpdate() => Assert.Equal("Alice Updated", _result.Data!.DisplayName);
}

// ── UpdateAppUserAsync — user not found ───────────────────────────────────────

public class AppUserService_UpdateAppUserAsync_WhenNotFound
{
    private readonly Result<UserResponseDto> _result;

    public AppUserService_UpdateAppUserAsync_WhenNotFound()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        repo.UpdateAppUserAsync(Arg.Any<Guid>(), Arg.Any<UpdateAppUserRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<AppUser>.Failure(ErrorCode.UserNotFound));
        _result = sut.UpdateAppUserAsync(Guid.NewGuid(), new UpdateAppUserRequestDto())
            .GetAwaiter().GetResult();
    }

    [Fact] public void IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCodeIsUserNotFound() => Assert.Equal(ErrorCode.UserNotFound, _result.ErrorCode);
}

// ── DeleteAppUserAsync — succeeds ─────────────────────────────────────────────

public class AppUserService_DeleteAppUserAsync_WhenSuccess
{
    private readonly Result<bool> _result;

    public AppUserService_DeleteAppUserAsync_WhenSuccess()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        repo.DeleteAppUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));
        _result = sut.DeleteAppUserAsync(Guid.NewGuid()).GetAwaiter().GetResult();
    }

    [Fact] public void IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void DataIsTrue() => Assert.True(_result.Data);
}

// ── BanAppUserAsync — succeeds ────────────────────────────────────────────────

public class AppUserService_BanAppUserAsync_WhenSuccess
{
    private readonly Result<bool> _result;

    public AppUserService_BanAppUserAsync_WhenSuccess()
    {
        var (sut, repo) = AppUserSutFactory.Create();
        repo.BanAppUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));
        _result = sut.BanAppUserAsync(Guid.NewGuid()).GetAwaiter().GetResult();
    }

    [Fact] public void IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void DataIsTrue() => Assert.True(_result.Data);
}
