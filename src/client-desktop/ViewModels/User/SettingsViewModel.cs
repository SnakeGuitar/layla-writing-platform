using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Services;
using Layla.Desktop.Services.User.Authentication;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Layla.Desktop.ViewModels.User;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    // ── Profile ────────────────────────────────────────────────────────────────

    [ObservableProperty] private string _profileDisplayName = SessionManager.CurrentDisplayName;
    [ObservableProperty] private string _profileBio = string.Empty;
    [ObservableProperty] private string? _profileAvatarUrl = SessionManager.CurrentAvatarUrl;
    [ObservableProperty] private string _profileSaveStatus = string.Empty;
    [ObservableProperty] private bool _isSavingProfile;

    public string ProfileEmail => SessionManager.CurrentEmail;

    /// <summary>Initials shown when no avatar is set (up to 2 chars from DisplayName).</summary>
    public string ProfileInitials
    {
        get
        {
            var name = ProfileDisplayName.Trim();
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : name[..Math.Min(2, name.Length)].ToUpper();
        }
    }

    public bool HasAvatar => !string.IsNullOrEmpty(ProfileAvatarUrl);

    // ── Appearance ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _currentTheme = string.Empty;

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private string _serverCoreUrl = ConfigurationService.SERVER_CORE_URL;

    [ObservableProperty]
    private string _worldbuildingUrl = ConfigurationService.WORLDBUILDING_API_URL;

    [ObservableProperty]
    private string _connectionSaveStatus = string.Empty;

    public string AppVersion => "1.0.0";
    public string MilestoneVersion => "1.0.0";
    public string MilestoneLabel => "v1.0 — MVP Entrega Final";
    public string VersionStatus => AppVersion == MilestoneVersion
        ? "✔ Al día con el milestone"
        : $"⚠ Pendiente — objetivo: {MilestoneVersion}";
    public bool IsVersionOnTarget => AppVersion == MilestoneVersion;

    public SettingsViewModel() : this(ServiceLocator.GetService<IAuthService>()
        ?? throw new InvalidOperationException("IAuthService not registered")) { }

    public SettingsViewModel(IAuthService authService)
    {
        _authService = authService;
        if (Application.Current is App app)
        {
            _currentTheme = app.CurrentTheme;
            _isFullscreen = app.IsFullscreen;
        }
        _serverCoreUrl = ConfigurationService.SERVER_CORE_URL;
        _worldbuildingUrl = ConfigurationService.WORLDBUILDING_API_URL;
    }

    partial void OnCurrentThemeChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            (Application.Current as App)?.ChangeTheme(value);
        }
    }

    partial void OnIsFullscreenChanged(bool value)
    {
        (Application.Current as App)?.SetFullscreen(value);
    }

    [RelayCommand]
    private void SaveConnection()
    {
        if (string.IsNullOrWhiteSpace(ServerCoreUrl) || string.IsNullOrWhiteSpace(WorldbuildingUrl))
        {
            ConnectionSaveStatus = "URLs cannot be empty.";
            return;
        }
        ConfigurationService.Save(ServerCoreUrl, WorldbuildingUrl);
        ConnectionSaveStatus = "✔ Saved — restart the app to reconnect.";
    }

    partial void OnProfileAvatarUrlChanged(string? value)
    {
        OnPropertyChanged(nameof(HasAvatar));
    }

    partial void OnProfileDisplayNameChanged(string value)
    {
        OnPropertyChanged(nameof(ProfileInitials));
    }

    [RelayCommand]
    private void PickAvatar()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar avatar",
            Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.gif;*.webp)|*.png;*.jpg;*.jpeg;*.gif;*.webp"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var bytes = File.ReadAllBytes(dialog.FileName);
            var ext = Path.GetExtension(dialog.FileName).TrimStart('.').ToLower();
            var mime = ext switch { "jpg" or "jpeg" => "image/jpeg", "gif" => "image/gif", "webp" => "image/webp", _ => "image/png" };
            ProfileAvatarUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            ProfileSaveStatus = string.Empty;
        }
        catch (Exception ex)
        {
            ProfileSaveStatus = $"No se pudo cargar la imagen: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveAvatar()
    {
        ProfileAvatarUrl = string.Empty;
        ProfileSaveStatus = string.Empty;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (IsSavingProfile) return;
        IsSavingProfile = true;
        ProfileSaveStatus = "Guardando...";

        var (success, error) = await _authService.UpdateProfileAsync(
            string.IsNullOrWhiteSpace(ProfileDisplayName) ? null : ProfileDisplayName.Trim(),
            ProfileBio,
            ProfileAvatarUrl  // null = unchanged, "" = clear, "data:..." = new image
        );

        ProfileSaveStatus = success ? "✔ Perfil actualizado" : $"Error: {error}";
        IsSavingProfile = false;
    }

    [RelayCommand]
    private void GoBack()
    {
        OnRequestGoBack?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OnRequestGoBack;
}
