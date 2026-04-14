using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models.Authentication;
using Layla.Desktop.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Layla.Desktop.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _signUpButtonContent = "Sign Up";

        public event EventHandler? OnRegistrationSuccess;
        public event EventHandler? OnNavigateToLogin;

        public SignUpViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task SignUpAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(DisplayName))
            {
                StatusMessage = "Please fill in all fields.";
                return;
            }

            IsBusy = true;
            SignUpButtonContent = "Creating account...";
            StatusMessage = string.Empty;

            try
            {
                var request = new RegisterRequest
                {
                    Email = Email,
                    Password = Password,
                    DisplayName = DisplayName
                };

                var response = await _authService.RegisterAsync(request);

                if (response.IsSuccess && response.Response != null)
                {
                    SessionManager.CurrentToken = response.Response.Token;
                    SessionManager.CurrentEmail = response.Response.Email;
                    SessionManager.CurrentDisplayName = response.Response.DisplayName;
                    OnRegistrationSuccess?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    StatusMessage = response.ErrorMessage ?? "Failed to create account.";
                    if (response.ValidationErrors.Count > 0)
                    {
                        StatusMessage = string.Join("\n", response.ValidationErrors.SelectMany(v => v.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                SignUpButtonContent = "Sign Up";
            }
        }

        [RelayCommand]
        private void NavigateToLogin()
        {
            OnNavigateToLogin?.Invoke(this, EventArgs.Empty);
        }
    }
}
