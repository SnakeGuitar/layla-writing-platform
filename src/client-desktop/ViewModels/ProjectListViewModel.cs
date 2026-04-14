using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Layla.Desktop.ViewModels
{
    public partial class ProjectListViewModel : ObservableObject
    {
        private readonly IProjectApiService _projectApiService;

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isCreateModalVisible;

        [ObservableProperty]
        private bool _isEditModalVisible;

        [ObservableProperty]
        private string _newProjectTitle = string.Empty;

        [ObservableProperty]
        private string _newProjectGenre = string.Empty;

        [ObservableProperty]
        private string _newProjectSynopsis = string.Empty;

        [ObservableProperty]
        private bool _newProjectIsPublic;

        [ObservableProperty]
        private string _editProjectTitle = string.Empty;

        [ObservableProperty]
        private string _editProjectGenre = string.Empty;

        [ObservableProperty]
        private string _editProjectSynopsis = string.Empty;

        [ObservableProperty]
        private bool _editProjectIsPublic;

        [ObservableProperty]
        private string _createError = string.Empty;

        [ObservableProperty]
        private string _editError = string.Empty;

        private Guid _editingProjectId;

        public event EventHandler? OnLogout;

        public ProjectListViewModel(IProjectApiService projectApiService)
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
                OnLogout?.Invoke(this, EventArgs.Empty);
            });
        }

        [RelayCommand]
        public async Task LoadProjectsAsync()
        {
            IsLoading = true;
            try
            {
                var result = await _projectApiService.GetMyProjectsAsync();
                Projects.Clear();
                if (result != null)
                {
                    foreach (var project in result)
                    {
                        Projects.Add(project);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void OpenCreateModal()
        {
            NewProjectTitle = string.Empty;
            NewProjectGenre = string.Empty;
            NewProjectSynopsis = string.Empty;
            NewProjectIsPublic = false;
            CreateError = string.Empty;
            IsCreateModalVisible = true;
        }

        [RelayCommand]
        private void CloseCreateModal() => IsCreateModalVisible = false;

        [RelayCommand]
        private async Task CreateProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProjectTitle) || string.IsNullOrWhiteSpace(NewProjectGenre) || string.IsNullOrWhiteSpace(NewProjectSynopsis))
            {
                CreateError = "Please fill in all fields.";
                return;
            }

            try
            {
                var request = new CreateProjectRequest
                {
                    Title = NewProjectTitle,
                    LiteraryGenre = NewProjectGenre,
                    Synopsis = NewProjectSynopsis,
                    IsPublic = NewProjectIsPublic
                };

                var newProject = await _projectApiService.CreateProjectAsync(request);
                if (newProject != null)
                {
                    IsCreateModalVisible = false;
                    await LoadProjectsAsync();
                }
                else
                {
                    CreateError = "Failed to create project.";
                }
            }
            catch (Exception ex)
            {
                CreateError = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void OpenEditModal(Project project)
        {
            _editingProjectId = project.Id;
            EditProjectTitle = project.Title;
            EditProjectGenre = project.LiteraryGenre;
            EditProjectSynopsis = project.Synopsis;
            EditProjectIsPublic = project.IsPublic;
            EditError = string.Empty;
            IsEditModalVisible = true;
        }

        [RelayCommand]
        private void CloseEditModal() => IsEditModalVisible = false;

        [RelayCommand]
        private async Task UpdateProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(EditProjectTitle) || string.IsNullOrWhiteSpace(EditProjectGenre) || string.IsNullOrWhiteSpace(EditProjectSynopsis))
            {
                EditError = "Please fill in all fields.";
                return;
            }

            try
            {
                var request = new UpdateProjectRequest
                {
                    Title = EditProjectTitle,
                    LiteraryGenre = EditProjectGenre,
                    Synopsis = EditProjectSynopsis,
                    IsPublic = EditProjectIsPublic
                };

                var updated = await _projectApiService.UpdateProjectAsync(_editingProjectId, request);
                if (updated != null)
                {
                    IsEditModalVisible = false;
                    await LoadProjectsAsync();
                }
                else
                {
                    EditError = "Failed to update project.";
                }
            }
            catch (Exception ex)
            {
                EditError = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteProjectAsync(Project project)
        {
            var result = MessageBox.Show($"Are you sure you want to delete '{project.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                bool deleted = await _projectApiService.DeleteProjectAsync(project.Id);
                if (deleted) await LoadProjectsAsync();
                else MessageBox.Show("Failed to delete project.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
