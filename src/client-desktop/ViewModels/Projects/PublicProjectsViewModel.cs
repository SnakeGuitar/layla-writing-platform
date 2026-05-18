using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models.Projects;
using Layla.Desktop.Services.Projetcs;
using System.Collections.ObjectModel;
using System.Windows;

namespace Layla.Desktop.ViewModels.Projects;

public partial class PublicProjectsViewModel : ObservableObject
{
    private readonly IProjectApiService _projectApiService;

    [ObservableProperty]
    private ObservableCollection<Project> _publicProjects = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Project? _selectedProject;

    public event EventHandler? OnBackToMyProjects;
    public event EventHandler<Project>? OnOpenProject;

    public PublicProjectsViewModel(IProjectApiService projectApiService)
    {
        _projectApiService = projectApiService;
    }

    [RelayCommand]
    public async Task LoadPublicProjectsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _projectApiService.GetPublicProjectsAsync();
            PublicProjects.Clear();
            if (result != null)
            {
                foreach (var project in result)
                {
                    PublicProjects.Add(project);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading public projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task JoinProjectAsync(Project project)
    {
        try
        {
            var result = await _projectApiService.JoinPublicProjectAsync(project.Id);
            if (result != null)
            {
                MessageBox.Show($"You joined '{project.Title}' as a reader!", "Joined", MessageBoxButton.OK, MessageBoxImage.Information);
                OnOpenProject?.Invoke(this, project);
            }
            else
            {
                MessageBox.Show("Could not join the project. You may already be a member.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ViewProject(Project project)
    {
        OnOpenProject?.Invoke(this, project);
    }

    [RelayCommand]
    private void BackToMyProjects()
    {
        OnBackToMyProjects?.Invoke(this, EventArgs.Empty);
    }
}
