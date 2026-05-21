using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models.Projects;
using Layla.Desktop.Services;
using Layla.Desktop.Services.Logger;
using Layla.Desktop.Services.Projetcs;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace Layla.Desktop.ViewModels.Projects;

public partial class WorkspaceViewModel : ObservableObject
{
    private readonly IProjectApiService _projectApiService;
    private DispatcherTimer? _heartbeatTimer;
    private readonly ILogger<WorkspaceViewModel> _logger;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private bool _isCollaboratorsModalVisible;

    [ObservableProperty]
    private ObservableCollection<Collaborator> _collaborators = new();

    [ObservableProperty]
    private string _inviteEmail = string.Empty;

    [ObservableProperty]
    private string _inviteError = string.Empty;

    [ObservableProperty]
    private bool _isInviting;

    public event EventHandler? OnLogout;
    public event EventHandler? OnBackToProjects;
    public event EventHandler? OnSettings;

    public WorkspaceViewModel(IProjectApiService projectApiService)
    {
        _projectApiService = projectApiService;
        _projectApiService.SessionDisplaced += OnSessionDisplaced;
        _logger = Log.For<WorkspaceViewModel>();
    }

    private void OnSessionDisplaced()
    {
        _logger.LogError("OnSessionDisplaced() - Session displaced:\n\tUser logged in from another device.");
        App.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                "Sesión terminada: Se ha iniciado sesión en otro dispositivo con esta cuenta.",
                "Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
            Logout();
        });
    }

    public void Initialize(Project project)
    {
        CurrentProject = project;
        StartHeartbeat();
    }

    private void StartHeartbeat()
    {
        _heartbeatTimer = new();
        _heartbeatTimer.Interval = TimeSpan.FromSeconds(30);
        _heartbeatTimer.Tick += async (s, e) => await SendHeartbeat();
        _heartbeatTimer.Start();
        _ = SendHeartbeat();
        _logger.LogTrace("StartHeartbeat() - Heartbeat started for project {ProjectId}", CurrentProject?.Id);
    }

    private async Task SendHeartbeat()
    {
        if (CurrentProject is null) return;
        await _projectApiService.ConnectPresenceHubAsync();
        await _projectApiService.AuthorHeartbeatAsync(CurrentProject.Id);

        _logger.LogTrace("SendHeartbeat() - Heartbeat sent for project {ProjectId}", CurrentProject.Id);
    }

    private void StopHeartbeat()
    {
        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer = null;
            _logger.LogTrace("StopHeartbeat() - Heartbeat stopped for project {ProjectId}", CurrentProject?.Id);
        }
        _logger.LogTrace("StopHeartbeat() - No heartbeat to stop for project {ProjectId}", CurrentProject?.Id);
    }

    // The IProjectApiService is a Singleton and we subscribe to its
    // SessionDisplaced event in the ctor — without explicit Dispose the
    // Singleton retains every WorkspaceViewModel (Transient) forever.
    public void Dispose()
    {
        StopHeartbeat();
        _projectApiService.SessionDisplaced -= OnSessionDisplaced;
        GC.SuppressFinalize(this);
        _logger.LogTrace("Dispose() - WorkspaceViewModel disposed for project {ProjectId}", CurrentProject?.Id);
    }


    [RelayCommand]
    private void Logout()
    {
        SessionManager.ClearSession();
        StopHeartbeat();
        OnLogout?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation("Logout() - User logged out and session cleared.");
    }

    [RelayCommand]
    private void BackToProjects()
    {
        StopHeartbeat();
        OnBackToProjects?.Invoke(this, EventArgs.Empty);

        _logger.LogTrace("BackToProjects() - Navigating back to projects list from project {ProjectId}", CurrentProject?.Id);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        OnSettings?.Invoke(this, EventArgs.Empty);

        _logger.LogTrace("OpenSettings() - Navigating to settings from project {ProjectId}", CurrentProject?.Id);
    }

    [RelayCommand]
    private async Task OpenCollaboratorsModalAsync()
    {
        InviteEmail = string.Empty;
        InviteError = string.Empty;
        IsCollaboratorsModalVisible = true;
        await LoadCollaboratorsAsync();

        _logger.LogTrace("OpenCollaboratorsModalAsync() - Collaborators modal opened for project {ProjectId}", CurrentProject?.Id);
    }

    [RelayCommand]
    private void CloseCollaboratorsModal()
    {
        IsCollaboratorsModalVisible = false;
        _logger.LogTrace("CloseCollaboratorsModalAsync() - Collaborators modal closed for project {ProjectId}", CurrentProject?.Id);
    }

    [RelayCommand]
    private async Task LoadCollaboratorsAsync()
    {
        if (CurrentProject == null) return;

        try
        {
            IEnumerable<Collaborator>? result = await _projectApiService.GetCollaboratorsAsync(CurrentProject.Id);
            Collaborators.Clear();
            if (result != null)
            {
                foreach (Collaborator c in result)
                    Collaborators.Add(c);
            }

            _logger.LogTrace("LoadCollaboratorsAsync() - Loaded {Count} collaborators for project {ProjectId}", Collaborators.Count, CurrentProject.Id);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("LoadCollaboratoAsyn() - Method exception:\n\t{ex}", ex.ToString());
        }
    }

    [RelayCommand]
    private async Task InviteCollaboratorAsync()
    {
        if (CurrentProject == null) return;

        if (string.IsNullOrWhiteSpace(InviteEmail))
        {
            InviteError = "Please enter an email address.";
            _logger.LogWarning("InviteCollaboratorAsync() - Email required.");
            return;
        }

        IsInviting = true;
        InviteError = string.Empty;

        try
        {
            InviteCollaboratorRequest request = new()
            {
                Email = InviteEmail,
                Role = "READER"
            };

            Collaborator? result = await _projectApiService.InviteCollaboratorAsync(CurrentProject.Id, request);
            if (result != null)
            {
                InviteEmail = string.Empty;
                await LoadCollaboratorsAsync();
                _logger.LogTrace("InviteCollaboratorAsyn() - Invitation send.");
            }
            else
            {
                _logger.LogWarning("InviteCollaboratorAsync() - User email {email} not found.", request.Email);
                InviteError = "Could not invite user. Check the email and try again.";
            }
        }
        catch (Exception ex)
        {
            InviteError = $"Error: {ex.Message}";
            _logger.LogCritical("InviteCollaboratorAsync() - Method exception:\n\t{ex}", ex.ToString());
        }
        finally
        {
            IsInviting = false;
        }
    }

    [RelayCommand]
    private async Task RemoveCollaboratorAsync(Collaborator collaborator)
    {
        if (CurrentProject == null) return;

        MessageBoxResult confirm = MessageBox.Show(
            $"Remove {collaborator.DisplayName ?? collaborator.Email} from this project?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            bool success = await _projectApiService.RemoveCollaboratorAsync(CurrentProject.Id, collaborator.UserId);
            if (success)
            {
                await LoadCollaboratorsAsync();
                _logger.LogTrace("RemoveCollaboratorAsync() - Collaborator removed.") }
            else
            {
                MessageBox.Show(
                    "Failed to remove collaborator.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _logger.LogError("RemoveCollaboratorAsync() - Cannot remove collaborator.");
            }
        }
    }
}
