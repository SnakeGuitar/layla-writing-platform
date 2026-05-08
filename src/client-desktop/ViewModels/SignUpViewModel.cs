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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSignUpStep))]
        private bool _isVerificationStep;

        public bool IsSignUpStep => !IsVerificationStep;

        [ObservableProperty]
        private string _verificationPin = string.Empty;

        [ObservableProperty]
        private string _verifyButtonContent = "Verify Email";

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

                if (response.IsSuccess)
                {
                    IsVerificationStep = true;
                    StatusMessage = "A verification PIN has been sent to your email.";
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
        private async Task VerifyAsync()
        {
            if (string.IsNullOrWhiteSpace(VerificationPin))
            {
                StatusMessage = "Please enter the verification PIN.";
                return;
            }

            IsBusy = true;
            VerifyButtonContent = "Verifying...";
            StatusMessage = string.Empty;

            try
            {
                var request = new VerifyEmailRequest
                {
                    Email = Email,
                    Pin = VerificationPin
                };

                var response = await _authService.VerifyEmailAsync(request);

                if (response.IsSuccess && response.Response != null)
                {
                    SessionManager.SaveSession(
                        response.Response.Token,
                        response.Response.Email,
                        response.Response.DisplayName,
                        response.Response.UserId);
                    OnRegistrationSuccess?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    StatusMessage = response.ErrorMessage ?? "Failed to verify PIN.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                VerifyButtonContent = "Verify Email";
            }
        }

        [RelayCommand]
        private void NavigateToLogin()
        {
            OnNavigateToLogin?.Invoke(this, EventArgs.Empty);
        }
    }
}
