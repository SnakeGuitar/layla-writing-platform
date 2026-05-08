using client_web.Application.Schemas.Auth;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace client_web.Application.Services.Session;

/// <summary>
/// Blazor Server implementation of <see cref="ISessionManager"/>.
/// Persists the session to <see cref="ProtectedSessionStorage"/> (encrypted
/// per-server, scoped to the browser tab) so a page reload keeps the user
/// signed in for the lifetime of that tab.
/// </summary>
public class SessionManager : ISessionManager
{
    private const string StorageKey = "layla.session";

    private readonly ProtectedSessionStorage _storage;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(ProtectedSessionStorage storage, ILogger<SessionManager> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public string CurrentToken { get; private set; } = string.Empty;
    public string CurrentUserId { get; private set; } = string.Empty;
    public string CurrentEmail { get; private set; } = string.Empty;
    public string CurrentDisplayName { get; private set; } = string.Empty;
    public DateTime? ExpiresAt { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(CurrentToken)
        && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow);

    public event Action? SessionChanged;

    public async Task<bool> InitializeAsync()
    {
        try
        {
            var stored = await _storage.GetAsync<StoredSession>(StorageKey);
            if (!stored.Success || stored.Value is null) return false;

            // Honor token expiry — anything stale is dropped on hydrate.
            if (stored.Value.ExpiresAt is { } exp && exp <= DateTime.UtcNow)
            {
                await ClearAsync();
                return false;
            }

            ApplyInMemory(stored.Value);
            SessionChanged?.Invoke();
            return IsAuthenticated;
        }
        catch (InvalidOperationException)
        {
            // Thrown when JS interop isn't available yet (prerender phase).
            // Caller will retry from OnAfterRenderAsync.
            return false;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Could not hydrate session from ProtectedSessionStorage.");
            return false;
        }
    }

    public async Task SaveAsync(LoginResponse response)
    {
        var snapshot = new StoredSession
        {
            Token = response.Token,
            UserId = response.UserId,
            Email = response.Email,
            DisplayName = response.DisplayName,
            ExpiresAt = response.ExpiresAt == default ? null : response.ExpiresAt,
        };
        ApplyInMemory(snapshot);

        try
        {
            await _storage.SetAsync(StorageKey, snapshot);
        }
        catch (Exception ex)
        {
            // Persistence failure is non-fatal — the session is still valid in memory
            // for the current circuit, just not across reloads.
            _logger.LogWarning(ex, "Could not persist session to ProtectedSessionStorage.");
        }

        SessionChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        CurrentToken = string.Empty;
        CurrentUserId = string.Empty;
        CurrentEmail = string.Empty;
        CurrentDisplayName = string.Empty;
        ExpiresAt = null;

        try
        {
            await _storage.DeleteAsync(StorageKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not clear session from ProtectedSessionStorage.");
        }

        SessionChanged?.Invoke();
    }

    private void ApplyInMemory(StoredSession snapshot)
    {
        CurrentToken = snapshot.Token ?? string.Empty;
        CurrentUserId = snapshot.UserId ?? string.Empty;
        CurrentEmail = snapshot.Email ?? string.Empty;
        CurrentDisplayName = snapshot.DisplayName ?? string.Empty;
        ExpiresAt = snapshot.ExpiresAt;
    }

    /// <summary>Serialised shape inside <see cref="ProtectedSessionStorage"/>.</summary>
    private sealed class StoredSession
    {
        public string? Token { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
