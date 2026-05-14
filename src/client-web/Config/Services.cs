using client_web.Application.Config.Http;
using client_web.Application.Config.SignalR;
using client_web.Application.Services.ActiveStatusAuthor;
using client_web.Application.Services.Admin;
using client_web.Application.Services.Auth;
using client_web.Application.Services.Projects;
using client_web.Application.Services.Session;
using client_web.Application.Services.Voice;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace client_web.Config;

public static class Services
{
    public static void Configure(this IServiceCollection services)
    {
        // ── HTTP ─────────────────────────────────────────────────────────────
        // ApiClient is registered as a *typed HttpClient* in HttpClientConfig
        // (with BaseAddress + Polly retry). Do NOT re-register it here — a plain
        // AddScoped<ApiClient>() would shadow the typed registration and inject
        // a default HttpClient with no BaseAddress, causing requests to fail
        // with "An invalid request URI was provided".

        // ── Session + Auth (mirrors the desktop SessionManager / AuthService) ──
        // ProtectedSessionStorage is registered automatically by AddRazorComponents,
        // but we register it explicitly so injection works in plain class consumers.
        services.AddScoped<ProtectedSessionStorage>();
        services.AddScoped<ISessionManager, SessionManager>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AuthenticationStateProvider, LaylaAuthenticationStateProvider>();

        // The HTTP authorization pipeline (auto-wired by MapRazorComponents
        // when CascadingAuthenticationState is registered) requires an
        // IAuthenticationService. The web client doesn't validate JWTs at the
        // HTTP layer — auth happens entirely inside Blazor — so we register a
        // stub scheme that always reports "no result". Component-level
        // AuthorizeView / AuthorizeRouteView remain in charge.
        services.AddAuthentication(NoopAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, NoopAuthenticationHandler>(
                    NoopAuthenticationHandler.SchemeName, _ => { });
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        // ── Domain services ──────────────────────────────────────────────────
        services.AddScoped<IPresenceService, PresenceService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAdminService, AdminService>();

        // ── Voice (transient SignalR-based stack) ────────────────────────────
        services.AddSingleton<ISignalRClient, SignalRClient>();
        services.AddSingleton<IVoiceService, VoiceService>();
        services.AddSingleton<Application.Services.Voice.IConnectionService>(sp => sp.GetRequiredService<IVoiceService>());
        services.AddSingleton<IRoomService>(sp => sp.GetRequiredService<IVoiceService>());
        services.AddSingleton<IAudioService>(sp => sp.GetRequiredService<IVoiceService>());
    }
}
