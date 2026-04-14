using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Layla.Desktop.ViewModels
{
    public partial class ReaderWorkspaceViewModel : ObservableObject
    {
        private readonly IProjectApiService _projectApiService;

        [ObservableProperty]
        private Project? _currentProject;

        [ObservableProperty]
        private bool _isAuthorActive;

        [ObservableProperty]
        private string _authorStatusText = "Author is offline";

        public event EventHandler? OnBackToPublicProjects;
        public event EventHandler? OnLogout;

        public ReaderWorkspaceViewModel(IProjectApiService projectApiService)
        {
            _projectApiService = projectApiService;
            _projectApiService.SessionDisplaced += OnSessionDisplaced;
        }

        private void OnSessionDisplaced()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Sessión terminada: Se ha iniciado sesión en otro dispositivo con esta cuenta.", 
                    "Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                Logout();
            });
        }

        public void Initialize(Project project)
        {
            CurrentProject = project;
            IsAuthorActive = project.IsAuthorActive;
            AuthorStatusText = project.IsAuthorActive ? "Author is active - live changes" : "Author is offline";
        }

        public async Task StartWatchingPresenceAsync()
        {
            if (CurrentProject == null) return;

            try
            {
                _projectApiService.AuthorStatusChanged += OnAuthorStatusChanged;
                
                await _projectApiService.ConnectPresenceHubAsync();
                await _projectApiService.WatchProjectAsync(CurrentProject.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting to presence hub: {ex.Message}");
            }
        }

        private void OnAuthorStatusChanged(Guid projectId, bool isActive)
        {
            if (CurrentProject != null && projectId == CurrentProject.Id)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    IsAuthorActive = isActive;
                    AuthorStatusText = isActive ? "Author is active - live changes" : "Author is offline";
                });
            }
        }

        [RelayCommand]
        private void BackToPublicProjects()
        {
            OnBackToPublicProjects?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Logout()
        {
            SessionManager.ClearSession();
            OnLogout?.Invoke(this, EventArgs.Empty);
        }
    }
}
