using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace client_web.Application.Services.Auth;

/// <summary>
/// Stub HTTP authentication handler.
///
/// The web client does not host any protected HTTP endpoints — every API call
/// is made from inside Blazor (server-side, with the JWT pulled from
/// <c>ISessionManager</c>) to <em>server-core</em>. We never validate JWTs on
/// our own request pipeline.
///
/// However, ASP.NET Core's authorisation middleware (auto-wired by
/// <c>MapRazorComponents</c> + <c>AddCascadingAuthenticationState</c>) demands
/// that <see cref="IAuthenticationService"/> be registered, otherwise it
/// throws <c>InvalidOperationException: Unable to find the required
/// 'IAuthenticationService' service</c>. This handler exists purely to
/// satisfy that registration. It always reports "no result" so the framework
/// falls back to anonymous, and Blazor's component-level auth (via
/// <see cref="LaylaAuthenticationStateProvider"/>) keeps full control over
/// who sees what.
/// </summary>
public sealed class NoopAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Noop";

    public NoopAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());

    /// <inheritdoc/>
    /// <remarks>
    /// Component-level <c>AuthorizeView</c> / <c>AuthorizeRouteView</c> handle
    /// redirects on the Blazor side via <c>RedirectToLogin</c>. The HTTP
    /// challenge is therefore a silent no-op — we just acknowledge the call.
    /// </remarks>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
