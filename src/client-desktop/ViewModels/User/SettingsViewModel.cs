using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Services;
using System.Windows;

namespace Layla.Desktop.ViewModels.User;

public partial class SettingsViewModel : ObservableObject
{
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

    public SettingsViewModel()
    {
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

    [RelayCommand]
    private void GoBack()
    {
        OnRequestGoBack?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OnRequestGoBack;
}
