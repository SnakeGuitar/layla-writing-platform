using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models.User.Authentication;
using Layla.Desktop.Services.Logger;
using Layla.Desktop.Services.User;
using Layla.Desktop.Services.User.Authentication;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.ViewModels.User;

public partial class SignUpViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<SignUpViewModel> _logger;

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
        _logger = Log.For<SignUpViewModel>();
    }

    [RelayCommand]
    private async Task SignUpAsync()
    {
        _logger.LogTrace("SignUpViewModel - SignUpAsync: Entering.");
        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(DisplayName))
        {
            StatusMessage = "Please fill in all fields.";
            _logger.LogWarning("SignUpViewModel - SignUpAsync: no data fields.");
            return;
        }

        IsBusy = true;
        SignUpButtonContent = "Creating account...";
        StatusMessage = string.Empty;

        try
        {
            RegisterRequest request = new RegisterRequest
            {
                Email = Email,
                Password = Password,
                DisplayName = DisplayName
            };

            _logger.LogTrace("SignUpViewModel - SignUpAsync: Calling service.");
            AuthResult response = await _authService.RegisterAsync(request);

            if (response.IsSuccess)
            {
                _logger.LogTrace("SignUpViewModel - SignUpAsync: Email sent.");
                IsVerificationStep = true;
                StatusMessage = "A verification PIN has been sent to your email.";
            }
            else
            {
                _logger.LogTrace("SignUpViewModel - SignUpAsync: Service response error.");
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
            _logger.LogError("SignUpViewModel - SignUpAsync: Method exception." + StatusMessage);
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
            VerifyEmailRequest request = new VerifyEmailRequest
            {
                Email = Email,
                Pin = VerificationPin
            };

            _logger.LogTrace("SignUpViewModel - VerifyAsync: Calling service.");
            AuthResult response = await _authService.VerifyEmailAsync(request);

            if (response.IsSuccess && response.Response != null)
            {
                _logger.LogTrace("SignUpViewModel - VerifyAsync: User registrered.");
                SessionManager.SaveSession(
                    response.Response.Token,
                    response.Response.Email,
                    response.Response.DisplayName,
                    response.Response.UserId);
                OnRegistrationSuccess?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _logger.LogTrace("SignUpViewModel - VerifyAsync: Service response error.");
                StatusMessage = response.ErrorMessage ?? "Failed to verify PIN.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("SignUpViewModel - VerifyAsync: Method exception.");
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
