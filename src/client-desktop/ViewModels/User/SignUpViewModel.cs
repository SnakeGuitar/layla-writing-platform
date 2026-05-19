using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models.User.Authentication;
using Layla.Desktop.Services;
using Layla.Desktop.Services.Logger;
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

    public bool IsSignUpStep => !this.IsVerificationStep;

    [ObservableProperty]
    private string _verificationPin = string.Empty;

    [ObservableProperty]
    private string _verifyButtonContent = "Verify Email";

    public event EventHandler? OnRegistrationSuccess;
    public event EventHandler? OnNavigateToLogin;

    public SignUpViewModel(IAuthService authService)
    {
        this._authService = authService;
        this._logger = Log.For<SignUpViewModel>();
    }

    [RelayCommand]
    private async Task SignUpAsync()
    {
        this._logger.LogTrace("SignUpAsync: Entering.");
        if (string.IsNullOrWhiteSpace(this.Email) ||
            string.IsNullOrWhiteSpace(this.Password) ||
            string.IsNullOrWhiteSpace(this.DisplayName))
        {
            this.StatusMessage = "Please fill in all fields.";
            this._logger.LogWarning("SignUpAsync: no data fields.");
            return;
        }

        this.IsBusy = true;
        this.SignUpButtonContent = "Creating account...";
        this.StatusMessage = string.Empty;

        try
        {
            RegisterRequest request = new()
            {
                Email = this.Email,
                Password = this.Password,
                DisplayName = this.DisplayName
            };

            this._logger.LogTrace("SignUpAsync: Calling service.");
            AuthResult response = await this._authService.RegisterAsync(request);

            if (response.IsSuccess)
            {
                this._logger.LogTrace("SignUpAsync: Email sent.");
                this.IsVerificationStep = true;
                this.StatusMessage = "A verification PIN has been sent to your email.";
            }
            else
            {
                this._logger.LogTrace("SignUpAsync: Service response error.");
                this.StatusMessage = response.ErrorMessage ?? "Failed to create account.";
                if (response.ValidationErrors.Count > 0)
                {
                    this.StatusMessage = string.Join("\n", response.ValidationErrors.SelectMany(v => v.Value));
                }
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Error: {ex.Message}";
            this._logger.LogError("SignUpAsync: Method exception." + this.StatusMessage);
        }
        finally
        {
            this.IsBusy = false;
            this.SignUpButtonContent = "Sign Up";
        }
    }

    [RelayCommand]
    private async Task VerifyAsync()
    {
        if (string.IsNullOrWhiteSpace(this.VerificationPin))
        {
            this.StatusMessage = "Please enter the verification PIN.";
            return;
        }

        this.IsBusy = true;
        this.VerifyButtonContent = "Verifying...";
        this.StatusMessage = string.Empty;

        try
        {
            VerifyEmailRequest request = new()
            {
                Email = this.Email,
                Pin = this.VerificationPin
            };

            this._logger.LogTrace("VerifyAsync: Calling service.");
            AuthResult response = await this._authService.VerifyEmailAsync(request);

            if (response.IsSuccess && response.Response != null)
            {
                this._logger.LogTrace("VerifyAsync: User registrered.");
                SessionManager.SaveSession(
                    response.Response.Token,
                    response.Response.Email,
                    response.Response.DisplayName,
                    response.Response.UserId);
                OnRegistrationSuccess?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this._logger.LogTrace("VerifyAsync: Service response error.");
                this.StatusMessage = response.ErrorMessage ?? "Failed to verify PIN.";
            }
        }
        catch (Exception ex)
        {
            this._logger.LogCritical("VerifyAsync: Method exception.\n\t" + ex.ToString());
            this.StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
            this.VerifyButtonContent = "Verify Email";
        }
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        OnNavigateToLogin?.Invoke(this, EventArgs.Empty);
    }
}
