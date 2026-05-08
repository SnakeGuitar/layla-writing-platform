using client_web.Application.Schemas.Auth;

namespace client_web.Application.Services.Session;

/// <summary>
/// Per-circuit session store for the authenticated user.
/// Mirrors the surface of <c>Layla.Desktop.Services.SessionManager</c>, with the
/// following adjustments for Blazor Server:
/// - Implementation is registered <c>Scoped</c> (one per SignalR circuit)
///   instead of <c>static</c>, so two browser tabs do not share state on the
///   server and concurrent users cannot collide.
/// - <see cref="InitializeAsync"/> and <see cref="SaveAsync"/> are async because
///   browser-storage hydration requires JS interop and must be called after
///   the first render.
/// </summary>
public interface ISessionManager
{
    string CurrentToken { get; }
    string CurrentUserId { get; }
    string CurrentEmail { get; }
    string CurrentDisplayName { get; }
    DateTime? ExpiresAt { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// Hydrates the in-memory session from persistent browser storage.
    /// No-op when called during prerender (JS interop is unavailable);
    /// callers should invoke this from <c>OnAfterRenderAsync(firstRender)</c>.
    /// </summary>
    /// <returns><c>true</c> if a valid session was restored.</returns>
    Task<bool> InitializeAsync();

    /// <summary>Persists the session to memory and browser storage.</summary>
    Task SaveAsync(LoginResponse response);

    /// <summary>Drops the session from memory and removes it from browser storage.</summary>
    Task ClearAsync();

    /// <summary>Raised whenever the authenticated identity changes (login, logout, refresh).</summary>
    event Action? SessionChanged;
}
