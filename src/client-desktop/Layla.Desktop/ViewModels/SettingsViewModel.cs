using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Layla.Desktop.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _currentTheme = string.Empty;

        [ObservableProperty]
        private bool _isFullscreen;

        public SettingsViewModel()
        {
            if (Application.Current is App app)
            {
                _currentTheme = app.CurrentTheme;
                _isFullscreen = app.IsFullscreen;
            }
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
        private void GoBack()
        {
            OnRequestGoBack?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? OnRequestGoBack;
    }
}
