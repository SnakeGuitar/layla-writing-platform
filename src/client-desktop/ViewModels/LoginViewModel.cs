using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models.Authentication;
using Layla.Desktop.Services;
using System;
using System.Threading.Tasks;

namespace Layla.Desktop.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

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
        }

        [RelayCommand]
        private async Task SignInAsync()
        {
            ErrorMessage = string.Empty;
            IsLoggingIn = true;
            LoginButtonContent = "Signing in...";

            try
            {
                var request = new LoginRequest { Email = Email, Password = Password };
                var response = await _authService.LoginAsync(request);

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
}
