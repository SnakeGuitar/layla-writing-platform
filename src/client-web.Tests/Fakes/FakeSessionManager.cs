using client_web.Application.Schemas.Auth;
using client_web.Application.Services.Session;

namespace ClientWeb.Tests.Fakes;

internal sealed class FakeSessionManager : ISessionManager
{
    public string CurrentToken { get; set; } = string.Empty;
    public string CurrentUserId { get; set; } = string.Empty;
    public string CurrentEmail { get; set; } = string.Empty;
    public string CurrentDisplayName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken) && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow);
    public bool WasCleared { get; private set; }

    public event Action? SessionChanged;

    public Task<bool> InitializeAsync() => Task.FromResult(IsAuthenticated);

    public Task SaveAsync(LoginResponse response)
    {
        CurrentToken = response.Token;
        CurrentUserId = response.UserId;
        CurrentEmail = response.Email;
        CurrentDisplayName = response.DisplayName;
        ExpiresAt = response.ExpiresAt;
        SessionChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        WasCleared = true;
        CurrentToken = string.Empty;
        CurrentUserId = string.Empty;
        CurrentEmail = string.Empty;
        CurrentDisplayName = string.Empty;
        ExpiresAt = null;
        SessionChanged?.Invoke();
        return Task.CompletedTask;
    }
}
