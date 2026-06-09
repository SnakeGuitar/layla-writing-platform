using client_web.Application.Schemas.Auth;
using client_web.Application.Services.Session;

namespace ClientWeb.Tests.Fakes;

internal sealed class FakeSessionManager : ISessionManager
{
    public string CurrentToken { get; set; } = string.Empty;
    public string CurrentUserId { get; set; } = string.Empty;
    public string CurrentEmail { get; set; } = string.Empty;
    public string CurrentDisplayName { get; set; } = string.Empty;
    public string CurrentAvatarUrl { get; set; } = string.Empty;
    public string CurrentBio { get; set; } = string.Empty;
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
        CurrentAvatarUrl = response.AvatarUrl ?? string.Empty;
        CurrentBio = response.Bio ?? string.Empty;
        ExpiresAt = response.ExpiresAt;
        SessionChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task UpdateProfileAsync(string? displayName, string? avatarUrl, string? bio)
    {
        CurrentDisplayName = displayName ?? CurrentDisplayName;
        CurrentAvatarUrl = avatarUrl ?? CurrentAvatarUrl;
        CurrentBio = bio ?? CurrentBio;
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
        CurrentAvatarUrl = string.Empty;
        CurrentBio = string.Empty;
        ExpiresAt = null;
        SessionChanged?.Invoke();
        return Task.CompletedTask;
    }
}
