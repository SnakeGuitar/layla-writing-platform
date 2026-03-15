using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using System;

namespace Layla.Desktop.ViewModels
{
    public partial class WorkspaceViewModel : ObservableObject
    {
        [ObservableProperty]
        private Project? _currentProject;

        public event EventHandler? OnLogout;
        public event EventHandler? OnBackToProjects;
        public event EventHandler? OnSettings;

        public WorkspaceViewModel()
        {
        }

        public void Initialize(Project project)
        {
            CurrentProject = project;
        }

        [RelayCommand]
        private void Logout()
        {
            Services.SessionManager.ClearSession();
            OnLogout?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void BackToProjects()
        {
            OnBackToProjects?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenSettings()
        {
            OnSettings?.Invoke(this, EventArgs.Empty);
        }
    }
}
