using BenchmarkDotNet.Attributes;
using Layla.Core.Configuration;
using Layla.Core.Constants;
using Layla.Core.Entities;
using Layla.Core.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

/// <summary>
/// Measures throughput of JWT generation and validation — the critical hot path
/// on every authenticated request that also issues a new token (login, token refresh).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class TokenBenchmarks
{
    private TokenService _service = null!;
    private AppUser _user = null!;
    private string _validToken = null!;
    private JwtSecurityTokenHandler _handler = null!;
    private TokenValidationParameters _validationParams = null!;

    private static readonly JwtSettings Settings = new()
    {
        Secret = "benchmark-secret-key-that-must-be-at-least-64-bytes-long-for-hmacsha512",
        Issuer = "layla-bench",
        Audience = "layla-bench",
        ExpirationInMinutes = 1440,
    };

    [GlobalSetup]
    public void Setup()
    {
        _service = new TokenService(Options.Create(Settings));

        _user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "bench@layla.io",
            DisplayName = "Benchmark User",
            TokenVersion = 1,
        };

        _validToken = _service.GenerateToken(_user, ["Writer"]);

        _handler = new JwtSecurityTokenHandler();
        _handler.InboundClaimTypeMap.Clear();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Secret));
        _validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Settings.Issuer,
            ValidateAudience = true,
            ValidAudience = Settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero,
        };
    }

    /// <summary>Full JWT signing cycle — symmetric key derivation + HMAC-SHA512 + Base64url encoding.</summary>
    [Benchmark(Baseline = true)]
    public string GenerateToken_SingleRole() =>
        _service.GenerateToken(_user, ["Writer"]);

    /// <summary>Same as above but with multiple roles — measures linear claim overhead.</summary>
    [Benchmark]
    public string GenerateToken_MultipleRoles() =>
        _service.GenerateToken(_user, ["Writer", "Admin", "Moderator"]);

    /// <summary>Full JWT validation cycle — signature verify + claims extraction + issuer/audience checks.</summary>
    [Benchmark]
    public ClaimsPrincipal ValidateToken()
    {
        _handler.InboundClaimTypeMap.Clear();
        return _handler.ValidateToken(_validToken, _validationParams, out _);
    }

    /// <summary>Raw JwtSecurityTokenHandler.ReadJwtToken — parsing only, no signature verification.</summary>
    [Benchmark]
    public JwtSecurityToken ParseTokenWithoutValidation() =>
        _handler.ReadJwtToken(_validToken);

    /// <summary>Round-trip: generate a fresh token then immediately validate it.</summary>
    [Benchmark]
    public ClaimsPrincipal GenerateAndValidateRoundtrip()
    {
        var token = _service.GenerateToken(_user, ["Writer"]);
        _handler.InboundClaimTypeMap.Clear();
        return _handler.ValidateToken(token, _validationParams, out _);
    }
}
