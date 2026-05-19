using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models.User.Authentication;
using Layla.Desktop.Services;
using Layla.Desktop.Services.Logger;
using Layla.Desktop.Services.User.Authentication;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.ViewModels.User;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoggingIn;

    [ObservableProperty]
    private string _loginButtonContent = "Login";

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        _logger = Log.For<LoginViewModel>();
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        this._logger.LogTrace("SignUpAsync: Entering.");
        ErrorMessage = string.Empty;
        IsLoggingIn = true;
        LoginButtonContent = "Signing in...";

        try
        {
            LoginRequest request = new() { Email = Email, Password = Password };
            AuthResult response = await _authService.LoginAsync(request);

            if (response.IsSuccess && response.Response != null)
            {
                SessionManager.SaveSession(
                    response.Response.Token,
                    response.Response.Email,
                    response.Response.DisplayName,
                    response.Response.UserId);

                // Fire an event or use a navigation service to signal success
                OnLoginSuccess?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = response.ErrorMessage ?? "Login failed.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoggingIn = false;
            LoginButtonContent = "Login";
        }
    }

    public event EventHandler? OnLoginSuccess;
}
