using System.Security.Claims;
using Layla.Api.Middleware;
using Layla.Core.Constants;
using Layla.Core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Layla.Core.Tests;

// ── helpers ───────────────────────────────────────────────────────────────────

file static class ValidatorSutFactory
{
    internal static (TokenVersionValidator Sut, UserManager<AppUser> Um) Create()
    {
        var store = Substitute.For<IUserStore<AppUser>>();
        var um = Substitute.For<UserManager<AppUser>>(store, null, null, null, null, null, null, null, null);
        return (new TokenVersionValidator(um, NullLogger<TokenVersionValidator>.Instance), um);
    }

    internal static TokenValidatedContext MakeContext(ClaimsPrincipal? principal)
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();
        return new TokenValidatedContext(httpContext, scheme, options) { Principal = principal };
    }

    internal static ClaimsPrincipal MakePrincipal(
        string? userId,
        int? tokenVersion,
        bool includeNameIdentifier = false)
    {
        var claims = new List<Claim>();
        if (userId != null) claims.Add(new Claim(ClaimNames.Sub, userId));
        if (tokenVersion.HasValue) claims.Add(new Claim(ClaimNames.TokenVersion, tokenVersion.Value.ToString()));
        if (includeNameIdentifier && userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}

// ── null principal ────────────────────────────────────────────────────────────

public class TokenVersionValidator_WhenPrincipalIsNull
{
    private readonly TokenValidatedContext _ctx;

    public TokenVersionValidator_WhenPrincipalIsNull()
    {
        var (sut, _) = ValidatorSutFactory.Create();
        _ctx = ValidatorSutFactory.MakeContext(null);
        sut.ValidateAsync(_ctx).GetAwaiter().GetResult();
    }

    [Fact] public void ContextFails() => Assert.NotNull(_ctx.Result!.Failure);
}

// ── missing sub/userId claim ──────────────────────────────────────────────────

public class TokenVersionValidator_WhenUserIdClaimMissing
{
    private readonly TokenValidatedContext _ctx;

    public TokenVersionValidator_WhenUserIdClaimMissing()
    {
        var (sut, _) = ValidatorSutFactory.Create();
        // Has tokenVersion but no userId
        _ctx = ValidatorSutFactory.MakeContext(ValidatorSutFactory.MakePrincipal(null, 1));
        sut.ValidateAsync(_ctx).GetAwaiter().GetResult();
    }

    [Fact] public void ContextFails() => Assert.NotNull(_ctx.Result!.Failure);
}

// ── missing token_version claim ───────────────────────────────────────────────

public class TokenVersionValidator_WhenTokenVersionClaimMissing
{
    private readonly TokenValidatedContext _ctx;

    public TokenVersionValidator_WhenTokenVersionClaimMissing()
    {
        var (sut, _) = ValidatorSutFactory.Create();
        // Has userId but no tokenVersion
        _ctx = ValidatorSutFactory.MakeContext(ValidatorSutFactory.MakePrincipal("user-1", null));
        sut.ValidateAsync(_ctx).GetAwaiter().GetResult();
    }

    [Fact] public void ContextFails() => Assert.NotNull(_ctx.Result!.Failure);
}

// ── user not found in database ────────────────────────────────────────────────

public class TokenVersionValidator_WhenUserNotFound
{
    private readonly TokenValidatedContext _ctx;

    public TokenVersionValidator_WhenUserNotFound()
    {
        var (sut, um) = ValidatorSutFactory.Create();
        um.FindByIdAsync(Arg.Any<string>()).Returns((AppUser?)null);
        _ctx = ValidatorSutFactory.MakeContext(ValidatorSutFactory.MakePrincipal("ghost-id", 1));
        sut.ValidateAsync(_ctx).GetAwaiter().GetResult();
    }

    [Fact] public void ContextFails() => Assert.NotNull(_ctx.Result!.Failure);
}

// ── token version mismatch (stale token) ─────────────────────────────────────

public class TokenVersionValidator_WhenTokenVersionMismatch
{
    private readonly TokenValidatedContext _ctx;

    public TokenVersionValidator_WhenTokenVersionMismatch()
    {
        var (sut, um) = ValidatorSutFactory.Create();
        // DB has version 5; token carries version 3 → stale
        um.FindByIdAsync("user-1").Returns(new AppUser { Id = "user-1", TokenVersion = 5 });
        _ctx = ValidatorSutFactory.MakeContext(ValidatorSutFactory.MakePrincipal("user-1", 3));
        sut.ValidateAsync(_ctx).GetAwaiter().GetResult();
    }

    [Fact] public void ContextFails() => Assert.NotNull(_ctx.Result!.Failure);
}

// ── valid token — version matches ─────────────────────────────────────────────

public class TokenVersionValidator_WhenTokenIsValid
{
    private readonly TokenValidatedContext _ctx;

    public TokenVersionValidator_WhenTokenIsValid()
    {
        var (sut, um) = ValidatorSutFactory.Create();
        um.FindByIdAsync("user-1").Returns(new AppUser { Id = "user-1", TokenVersion = 3 });
        _ctx = ValidatorSutFactory.MakeContext(ValidatorSutFactory.MakePrincipal("user-1", 3));
        sut.ValidateAsync(_ctx).GetAwaiter().GetResult();
    }

    // ctx.Result is null in .NET 10 when Fail was never called — use null-conditional
    [Fact] public void ContextDoesNotFail() => Assert.Null(_ctx.Result?.Failure);
}

// ── valid token — NameIdentifier added when absent ────────────────────────────

public class TokenVersionValidator_AddsNameIdentifierWhenAbsent
{
    private readonly ClaimsPrincipal _principal;

    public TokenVersionValidator_AddsNameIdentifierWhenAbsent()
    {
        var (sut, um) = ValidatorSutFactory.Create();
        um.FindByIdAsync("user-1").Returns(new AppUser { Id = "user-1", TokenVersion = 1 });
        // Principal has sub but NOT NameIdentifier
        _principal = ValidatorSutFactory.MakePrincipal("user-1", 1, includeNameIdentifier: false);
        var ctx = ValidatorSutFactory.MakeContext(_principal);
        sut.ValidateAsync(ctx).GetAwaiter().GetResult();
    }

    [Fact]
    public void NameIdentifierClaimIsAdded() =>
        Assert.True(_principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier));
}

// ── valid token — NameIdentifier not duplicated when already present ──────────

public class TokenVersionValidator_DoesNotDuplicateNameIdentifier
{
    private readonly ClaimsPrincipal _principal;

    public TokenVersionValidator_DoesNotDuplicateNameIdentifier()
    {
        var (sut, um) = ValidatorSutFactory.Create();
        um.FindByIdAsync("user-1").Returns(new AppUser { Id = "user-1", TokenVersion = 1 });
        _principal = ValidatorSutFactory.MakePrincipal("user-1", 1, includeNameIdentifier: true);
        var ctx = ValidatorSutFactory.MakeContext(_principal);
        sut.ValidateAsync(ctx).GetAwaiter().GetResult();
    }

    [Fact]
    public void NameIdentifierAppearsExactlyOnce() =>
        Assert.Equal(1, _principal.Claims.Count(c => c.Type == ClaimTypes.NameIdentifier));
}
