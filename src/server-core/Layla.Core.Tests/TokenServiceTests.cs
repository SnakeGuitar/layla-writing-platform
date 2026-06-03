using Layla.Core.Configuration;
using Layla.Core.Constants;
using Layla.Core.Entities;
using Layla.Core.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Layla.Core.Tests;

file static class TokenSutFactory
{
    internal static readonly JwtSettings DefaultSettings = new()
    {
        Secret = "test-secret-key-that-is-at-least-64-bytes-long-for-hmacsha512-algorithm",
        Issuer = "layla-tests",
        Audience = "layla-tests",
        ExpirationInMinutes = 60,
    };

    internal static TokenService Create(JwtSettings? settings = null) =>
        new(Options.Create(settings ?? DefaultSettings));

    internal static IReadOnlyList<Claim> ParseClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();
        return handler.ReadJwtToken(token).Claims.ToList();
    }
}

// ── GenerateToken — sub claim ─────────────────────────────────────────────────

public class TokenService_GenerateToken_SubClaim
{
    private readonly IReadOnlyList<Claim> _claims;

    public TokenService_GenerateToken_SubClaim()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "user-abc", Email = "a@b.com", DisplayName = "A" };
        _claims = TokenSutFactory.ParseClaims(svc.GenerateToken(user, []));
    }

    [Fact] public void Exists() => Assert.Contains(_claims, c => c.Type == "sub");
    [Fact] public void ValueEqualsUserId() => Assert.Equal("user-abc", _claims.First(c => c.Type == "sub").Value);
}

// ── GenerateToken — email claim ───────────────────────────────────────────────

public class TokenService_GenerateToken_EmailClaim
{
    private readonly IReadOnlyList<Claim> _claims;

    public TokenService_GenerateToken_EmailClaim()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "u1", Email = "writer@layla.io", DisplayName = "Writer" };
        _claims = TokenSutFactory.ParseClaims(svc.GenerateToken(user, []));
    }

    [Fact] public void Exists() => Assert.Contains(_claims, c => c.Type == "email");
    [Fact] public void ValueMatchesUserEmail() => Assert.Equal("writer@layla.io", _claims.First(c => c.Type == "email").Value);
}

// ── GenerateToken — tokenVersion claim ───────────────────────────────────────

public class TokenService_GenerateToken_TokenVersionClaim
{
    private readonly IReadOnlyList<Claim> _claims;

    public TokenService_GenerateToken_TokenVersionClaim()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "u2", Email = "b@c.com", DisplayName = "B", TokenVersion = 5 };
        _claims = TokenSutFactory.ParseClaims(svc.GenerateToken(user, []));
    }

    [Fact] public void Exists() => Assert.Contains(_claims, c => c.Type == ClaimNames.TokenVersion);
    [Fact] public void ValueMatchesTokenVersion() => Assert.Equal("5", _claims.First(c => c.Type == ClaimNames.TokenVersion).Value);
}

// ── GenerateToken — role claims ───────────────────────────────────────────────

public class TokenService_GenerateToken_WithMultipleRoles
{
    private readonly IReadOnlyList<Claim> _roleClaims;

    public TokenService_GenerateToken_WithMultipleRoles()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "u3", Email = "c@d.com", DisplayName = "C" };
        _roleClaims = TokenSutFactory.ParseClaims(svc.GenerateToken(user, ["Writer", "Admin"]))
            .Where(c => c.Type == ClaimNames.Role).ToList();
    }

    [Fact] public void ContainsWriterRole() => Assert.Contains(_roleClaims, c => c.Value == "Writer");
    [Fact] public void ContainsAdminRole() => Assert.Contains(_roleClaims, c => c.Value == "Admin");
    [Fact] public void ContainsExactlyTwoRoles() => Assert.Equal(2, _roleClaims.Count);
}

public class TokenService_GenerateToken_WithNoRoles
{
    private readonly IReadOnlyList<Claim> _roleClaims;

    public TokenService_GenerateToken_WithNoRoles()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "u4", Email = "d@e.com", DisplayName = "D" };
        _roleClaims = TokenSutFactory.ParseClaims(svc.GenerateToken(user, []))
            .Where(c => c.Type == ClaimNames.Role).ToList();
    }

    [Fact] public void RoleClaimsAreEmpty() => Assert.Empty(_roleClaims);
}

// ── GenerateToken — expiry ────────────────────────────────────────────────────

public class TokenService_GenerateToken_Expiry
{
    private readonly JwtSecurityToken _jwt;
    private readonly DateTime _before;

    public TokenService_GenerateToken_Expiry()
    {
        var settings = new JwtSettings
        {
            Secret = TokenSutFactory.DefaultSettings.Secret,
            Issuer = TokenSutFactory.DefaultSettings.Issuer,
            Audience = TokenSutFactory.DefaultSettings.Audience,
            ExpirationInMinutes = 30,
        };
        var svc = TokenSutFactory.Create(settings);
        var user = new AppUser { Id = "u5", Email = "e@f.com", DisplayName = "E" };
        _before = DateTime.UtcNow;
        _jwt = new JwtSecurityTokenHandler().ReadJwtToken(svc.GenerateToken(user, []));
    }

    [Fact] public void ExpiryIsAfterCurrentTime() => Assert.True(_jwt.ValidTo > _before);
    [Fact] public void ExpiryIsWithinConfiguredWindow() => Assert.True(_jwt.ValidTo <= _before.AddMinutes(31));
}

// ── GenerateToken — algorithm ─────────────────────────────────────────────────

public class TokenService_GenerateToken_Algorithm
{
    private readonly JwtSecurityToken _jwt;

    public TokenService_GenerateToken_Algorithm()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "u6", Email = "f@g.com", DisplayName = "F" };
        _jwt = new JwtSecurityTokenHandler().ReadJwtToken(svc.GenerateToken(user, []));
    }

    [Fact] public void AlgorithmIsHmacSha512() => Assert.Equal("HS512", _jwt.Header.Alg);
}

// ── GenerateToken — JTI uniqueness ────────────────────────────────────────────

public class TokenService_GenerateToken_JtiUniqueness
{
    private readonly string _jti1;
    private readonly string _jti2;

    public TokenService_GenerateToken_JtiUniqueness()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "u7", Email = "g@h.com", DisplayName = "G" };
        _jti1 = TokenSutFactory.ParseClaims(svc.GenerateToken(user, [])).First(c => c.Type == "jti").Value;
        _jti2 = TokenSutFactory.ParseClaims(svc.GenerateToken(user, [])).First(c => c.Type == "jti").Value;
    }

    [Fact] public void TwoConsecutiveTokensHaveDifferentJti() => Assert.NotEqual(_jti1, _jti2);
}

// ── GenerateToken — signature validity ───────────────────────────────────────

public class TokenService_GenerateToken_SignatureValidity
{
    private readonly ClaimsPrincipal _principal;

    public TokenService_GenerateToken_SignatureValidity()
    {
        var svc = TokenSutFactory.Create();
        var user = new AppUser { Id = "u8", Email = "h@i.com", DisplayName = "H" };
        var token = svc.GenerateToken(user, []);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenSutFactory.DefaultSettings.Secret));
        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TokenSutFactory.DefaultSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = TokenSutFactory.DefaultSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero,
        };

        _principal = handler.ValidateToken(token, validationParams, out _);
    }

    [Fact] public void TokenValidatesSuccessfully() => Assert.NotNull(_principal);
}

// ── Constructor — null secret ─────────────────────────────────────────────────

public class TokenService_Constructor_WithNullSecret
{
    [Fact]
    public void ThrowsInvalidOperationException()
    {
        var badSettings = new JwtSettings { Secret = null!, Issuer = "x", Audience = "x", ExpirationInMinutes = 60 };
        Assert.Throws<InvalidOperationException>(() => TokenSutFactory.Create(badSettings));
    }
}
