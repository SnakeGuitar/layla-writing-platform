using Layla.Desktop.Models.User.Authentication;
using Layla.Desktop.Models.User.Validation;
using Layla.Desktop.Services.Logger;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Layla.Desktop.Services.User.Authentication;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    public AuthService()
    {
        this._httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.SERVER_CORE_URL);
        this._logger = Log.For<AuthService>();
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        ValidationResult validation = request.Validate();
        if (!validation.IsValid)
        {
            _logger.LogWarning("LoginAsync: Validation failed for email {Email}.\n\tErrors: {Errors}",
                request.Email, string.Join(", ", validation.Errors.Select(
                    e => $"{e.Key}: {string.Join("; ", e.Value)}")));
            return AuthResult.ValidationError(validation.Errors, "Please correct the errors before logging in.");
        }

        try
        {
            _logger.LogTrace("LoginAsync: Calling service /api/tokens.");
            HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("/api/tokens", request);
            if (response.IsSuccessStatusCode)
            {
                AuthResponse? data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (data != null)
                {
                    _logger.LogTrace("LoginAsync: Data retrieved.");
                    return AuthResult.Success(data);
                }
                _logger.LogError("LoginAsync: Data retrieved.");
                return AuthResult.Fail("Invalid response from server.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogTrace("LoginAsync: Invalid fields.");
                return AuthResult.Fail("Invalid email or password.");
            }
            _logger.LogError("LoginAsync: Service /api/tokens no available.");
            return AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical("LoginAsync: Method exception.\n\t" + ex.ToString());
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        ValidationResult validation = request.Validate();
        if (!validation.IsValid)
        {
            _logger.LogTrace("RegisterAsync: Validation failed for email {Email}.\n\tErrors: {Errors}", request.Email, string.Join(", ", validation.Errors.Select(e => $"{e.Key}: {string.Join("; ", e.Value)}")));
            return AuthResult.ValidationError(validation.Errors, "Please correct the errors before registering.");
        }

        try
        {
            _logger.LogTrace("RegisterAsync: Calling service /api/users.");
            HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("/api/users", request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogTrace("RegisterAsync: Data retrieved.");
                AuthResponse? data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return data != null ? AuthResult.Success(data) : AuthResult.Fail("Invalid response from server.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning("RegisterAsync: Email is already registered.");
                return AuthResult.Fail("Email is already registered.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogError("RegisterAsync: Bad Request.");
                try
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Dictionary<string, List<string>>? errors =
                        JsonSerializer.Deserialize<Dictionary<string, List<string>>>(content);
                    if (errors != null && errors.Count > 0)
                    {
                        return AuthResult.ValidationError(errors, "Validation failed. Please check your input.");
                    }
                }
                catch
                {
                    return AuthResult.Fail("Validation failed.");
                }
            }

            _logger.LogError("RegisterAsync: Service /api/users no available.");
            return AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical("RegisterAsync: Method exception.\n\t" + ex.ToString());
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }

    public async Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request)
    {
        try
        {
            _logger.LogTrace("VerifyEmailAsync: Calling service /api/users/verify-email.");
            HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("/api/users/verify-email", request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogTrace("VerifyEmailAsync: Data retrieved.");
                AuthResponse? data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return data != null ? AuthResult.Success(data) : AuthResult.Fail("Invalid response from server.");
            }

            _logger.LogTrace("VerifyEmailAsync: Bad request.");
            return response.StatusCode == HttpStatusCode.BadRequest
                ? AuthResult.Fail("Invalid PIN.")
                : AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogTrace("VerifyEmailAsync: Method exception.\n\t" + ex.ToString());
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }
}
