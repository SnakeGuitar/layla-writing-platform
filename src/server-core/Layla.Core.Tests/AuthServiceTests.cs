using Layla.Core.Common;
using Layla.Core.Configuration;
using Layla.Core.Contracts.Auth;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Services;
using Layla.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Layla.Core.Tests;

file static class AuthSutFactory
{
    internal record Components(
        UserManager<AppUser> Um,
        SignInManager<AppUser> Sm,
        ITokenService Ts,
        IEmailService Es,
        AuthService Sut);

    internal static Components Create()
    {
        var store = Substitute.For<IUserStore<AppUser>>();
        var um = Substitute.For<UserManager<AppUser>>(store, null, null, null, null, null, null, null, null);
        var ctx = Substitute.For<IHttpContextAccessor>();
        var factory = Substitute.For<IUserClaimsPrincipalFactory<AppUser>>();
        var sm = Substitute.For<SignInManager<AppUser>>(um, ctx, factory, null, null, null, null);
        var ts = Substitute.For<ITokenService>();
        var es = Substitute.For<IEmailService>();
        var settings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-at-least-32-chars",
            Issuer = "test",
            Audience = "test",
            ExpirationInMinutes = 60,
        });
        return new(um, sm, ts, es, new AuthService(um, sm, ts, es, settings, NullLogger<AuthService>.Instance));
    }
}

// ── LoginAsync — user does not exist ─────────────────────────────────────────

public class AuthService_LoginAsync_WhenUserDoesNotExist
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_LoginAsync_WhenUserDoesNotExist()
    {
        var c = AuthSutFactory.Create();
        c.Um.FindByEmailAsync(Arg.Any<string>()).Returns((AppUser?)null);
        _result = c.Sut.LoginAsync(new LoginRequestDto { Email = "ghost@layla.io", Password = "pass" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidCredentials() => Assert.Equal(ErrorCode.InvalidCredentials, _result.ErrorCode);
}

// ── LoginAsync — account locked ───────────────────────────────────────────────

public class AuthService_LoginAsync_WhenAccountIsLockedOut
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_LoginAsync_WhenAccountIsLockedOut()
    {
        var c = AuthSutFactory.Create();
        var user = new AppUser { Id = "u1", Email = "locked@layla.io" };
        c.Um.FindByEmailAsync("locked@layla.io").Returns(user);
        c.Sm.CheckPasswordSignInAsync(user, Arg.Any<string>(), lockoutOnFailure: true)
            .Returns(SignInResult.LockedOut);
        _result = c.Sut.LoginAsync(new LoginRequestDto { Email = "locked@layla.io", Password = "pass" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsAccountLocked() => Assert.Equal(ErrorCode.AccountLocked, _result.ErrorCode);
}

// ── LoginAsync — wrong password ───────────────────────────────────────────────

public class AuthService_LoginAsync_WhenPasswordIsWrong
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_LoginAsync_WhenPasswordIsWrong()
    {
        var c = AuthSutFactory.Create();
        var user = new AppUser { Id = "u2", Email = "user@layla.io" };
        c.Um.FindByEmailAsync("user@layla.io").Returns(user);
        c.Sm.CheckPasswordSignInAsync(user, Arg.Any<string>(), lockoutOnFailure: true)
            .Returns(SignInResult.Failed);
        _result = c.Sut.LoginAsync(new LoginRequestDto { Email = "user@layla.io", Password = "badpass" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidCredentials() => Assert.Equal(ErrorCode.InvalidCredentials, _result.ErrorCode);
}

// ── LoginAsync — valid credentials ───────────────────────────────────────────

public class AuthService_LoginAsync_WhenCredentialsAreValid
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_LoginAsync_WhenCredentialsAreValid()
    {
        var c = AuthSutFactory.Create();
        var user = new AppUser { Id = "u3", Email = "ok@layla.io", DisplayName = "OK User", TokenVersion = 2 };
        c.Um.FindByEmailAsync("ok@layla.io").Returns(user);
        c.Sm.CheckPasswordSignInAsync(user, "correct", lockoutOnFailure: true).Returns(SignInResult.Success);
        c.Um.GetRolesAsync(user).Returns(new List<string> { "Writer" });
        c.Um.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        c.Ts.GenerateToken(Arg.Any<AppUser>(), Arg.Any<IList<string>>()).Returns("jwt-abc");
        _result = c.Sut.LoginAsync(new LoginRequestDto { Email = "ok@layla.io", Password = "correct" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void Token_EqualsGeneratedToken() => Assert.Equal("jwt-abc", _result.Data!.Token);
    [Fact] public void Email_MatchesInput() => Assert.Equal("ok@layla.io", _result.Data!.Email);
    [Fact] public void DisplayName_MatchesUser() => Assert.Equal("OK User", _result.Data!.DisplayName);
    [Fact] public void ExpiresAt_IsInTheFuture() => Assert.True(_result.Data!.ExpiresAt > DateTime.UtcNow);
}

// ── LoginAsync — token version increment ─────────────────────────────────────

public class AuthService_LoginAsync_TokenVersionBehavior
{
    private readonly UserManager<AppUser> _userManager;

    public AuthService_LoginAsync_TokenVersionBehavior()
    {
        var c = AuthSutFactory.Create();
        var user = new AppUser { Id = "u4", Email = "inc@layla.io", DisplayName = "Inc", TokenVersion = 1 };
        c.Um.FindByEmailAsync("inc@layla.io").Returns(user);
        c.Sm.CheckPasswordSignInAsync(user, Arg.Any<string>(), lockoutOnFailure: true).Returns(SignInResult.Success);
        c.Um.GetRolesAsync(user).Returns(new List<string>());
        c.Um.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        c.Ts.GenerateToken(Arg.Any<AppUser>(), Arg.Any<IList<string>>()).Returns("tok");
        c.Sut.LoginAsync(new LoginRequestDto { Email = "inc@layla.io", Password = "pass" })
            .GetAwaiter().GetResult();
        _userManager = c.Um;
    }

    [Fact]
    public void UpdateAsync_IsCalledWithIncrementedVersion() =>
        _userManager.Received(1).UpdateAsync(Arg.Is<AppUser>(u => u.TokenVersion == 2));
}

// ── LoginAsync — token version update fails ───────────────────────────────────

public class AuthService_LoginAsync_WhenTokenVersionUpdateFails
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_LoginAsync_WhenTokenVersionUpdateFails()
    {
        var c = AuthSutFactory.Create();
        var user = new AppUser { Id = "u5", Email = "fail@layla.io", DisplayName = "Fail" };
        c.Um.FindByEmailAsync("fail@layla.io").Returns(user);
        c.Sm.CheckPasswordSignInAsync(user, Arg.Any<string>(), lockoutOnFailure: true).Returns(SignInResult.Success);
        c.Um.GetRolesAsync(user).Returns(new List<string>());
        c.Um.UpdateAsync(Arg.Any<AppUser>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "db error" }));
        _result = c.Sut.LoginAsync(new LoginRequestDto { Email = "fail@layla.io", Password = "pass" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInternalError() => Assert.Equal(ErrorCode.InternalError, _result.ErrorCode);
}

// ── RegisterAsync — email already registered ──────────────────────────────────

public class AuthService_RegisterAsync_WhenEmailAlreadyRegistered
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_RegisterAsync_WhenEmailAlreadyRegistered()
    {
        var c = AuthSutFactory.Create();
        c.Um.FindByEmailAsync("exists@layla.io").Returns(new AppUser { Email = "exists@layla.io" });
        _result = c.Sut.RegisterAsync(new RegisterRequestDto { Email = "exists@layla.io", Password = "Pass1!" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsDuplicateEmail() => Assert.Equal(ErrorCode.DuplicateEmail, _result.ErrorCode);
}

// ── RegisterAsync — identity rejects password ────────────────────────────────

public class AuthService_RegisterAsync_WhenIdentityRejectsPassword
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_RegisterAsync_WhenIdentityRejectsPassword()
    {
        var c = AuthSutFactory.Create();
        c.Um.FindByEmailAsync(Arg.Any<string>()).Returns((AppUser?)null);
        c.Um.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password too short" }));
        _result = c.Sut.RegisterAsync(new RegisterRequestDto { Email = "new@layla.io", Password = "weak" })
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsValidationFailed() => Assert.Equal(ErrorCode.ValidationFailed, _result.ErrorCode);
}

// ── RegisterAsync — successful registration ───────────────────────────────────

public class AuthService_RegisterAsync_WhenSuccessful
{
    private readonly Result<AuthResponseDto> _result;

    public AuthService_RegisterAsync_WhenSuccessful()
    {
        var c = AuthSutFactory.Create();
        c.Um.FindByEmailAsync(Arg.Any<string>()).Returns((AppUser?)null);
        c.Um.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        c.Um.AddToRoleAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        c.Um.GenerateTwoFactorTokenAsync(Arg.Any<AppUser>(), "Email").Returns("654321");
        c.Es.SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        _result = c.Sut.RegisterAsync(new RegisterRequestDto
        {
            Email = "brand@layla.io",
            Password = "Strong1!",
            DisplayName = "Brand",
        }).GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void Token_IsEmpty_PendingEmailVerification() => Assert.Equal(string.Empty, _result.Data!.Token);
    [Fact] public void Email_MatchesInput() => Assert.Equal("brand@layla.io", _result.Data!.Email);
}

// ── RegisterAsync — email verification ───────────────────────────────────────

public class AuthService_RegisterAsync_EmailVerification
{
    private readonly IEmailService _emailService;

    public AuthService_RegisterAsync_EmailVerification()
    {
        var c = AuthSutFactory.Create();
        c.Um.FindByEmailAsync(Arg.Any<string>()).Returns((AppUser?)null);
        c.Um.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        c.Um.AddToRoleAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        c.Um.GenerateTwoFactorTokenAsync(Arg.Any<AppUser>(), "Email").Returns("123456");
        c.Es.SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        c.Sut.RegisterAsync(new RegisterRequestDto { Email = "verify@layla.io", Password = "Strong1!" })
            .GetAwaiter().GetResult();
        _emailService = c.Es;
    }

    [Fact]
    public void SendVerificationEmailAsync_IsCalledOnce() =>
        _emailService.Received(1).SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>());

    [Fact]
    public void VerificationEmail_IsSentToRegisteredAddress() =>
        _emailService.Received(1).SendVerificationEmailAsync("verify@layla.io", Arg.Any<string>());

    [Fact]
    public void VerificationEmail_UsesPinGeneratedByIdentity() =>
        _emailService.Received(1).SendVerificationEmailAsync(Arg.Any<string>(), "123456");
}

// ── RegisterAsync — display name fallback ────────────────────────────────────

public class AuthService_RegisterAsync_WhenDisplayNameOmitted
{
    private AppUser? _capturedUser;

    public AuthService_RegisterAsync_WhenDisplayNameOmitted()
    {
        var c = AuthSutFactory.Create();
        c.Um.FindByEmailAsync(Arg.Any<string>()).Returns((AppUser?)null);
        c.Um.CreateAsync(Arg.Do<AppUser>(u => _capturedUser = u), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        c.Um.AddToRoleAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        c.Um.GenerateTwoFactorTokenAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns("000000");
        c.Es.SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        c.Sut.RegisterAsync(new RegisterRequestDto
        {
            Email = "johndoe@layla.io",
            Password = "Strong1!",
            DisplayName = null,
        }).GetAwaiter().GetResult();
    }

    [Fact] public void DisplayName_DefaultsToEmailLocalPart() => Assert.Equal("johndoe", _capturedUser?.DisplayName);
}
