using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using client_web.Application.Services.Session;
using Microsoft.AspNetCore.Components.Authorization;

namespace client_web.Application.Services.Auth;

/// <summary>
/// Bridges <see cref="ISessionManager"/> into Blazor's authorisation pipeline.
///
/// On every <see cref="GetAuthenticationStateAsync"/> call this reads the
/// current token from the session, parses it as a JWT and projects the claims
/// into a <see cref="ClaimsPrincipal"/>. When the session changes (login,
/// logout, refresh) the provider raises <c>AuthenticationStateChanged</c> so
/// <c>&lt;CascadingAuthenticationState&gt;</c> consumers re-render.
/// </summary>
public class LaylaAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ISessionManager _session;
    private readonly JwtSecurityTokenHandler _jwtHandler = new();

    public LaylaAuthenticationStateProvider(ISessionManager session)
    {
        _session = session;
        _session.SessionChanged += OnSessionChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _session.CurrentToken;
        if (string.IsNullOrWhiteSpace(token) || !_session.IsAuthenticated)
            return Task.FromResult(Anonymous);

        try
        {
            // Read claims locally — server-core has already signed the token,
            // and Blazor only needs the claim set for [Authorize] / role gating.
            var jwt = _jwtHandler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(
                jwt.Claims,
                authenticationType: "jwt",
                nameType: "name",
                roleType: "role");

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
        catch
        {
            // Malformed token — treat as anonymous and let any sensitive page redirect.
            return Task.FromResult(Anonymous);
        }
    }

    private void OnSessionChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _session.SessionChanged -= OnSessionChanged;
        GC.SuppressFinalize(this);
    }
}
