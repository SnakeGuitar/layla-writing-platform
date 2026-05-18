using Layla.Desktop.Models.User.Authentication;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;

namespace Layla.Desktop.Services.User.Authentication;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    public AuthService()
    {
        _httpClient = ConfigurationService.CreateHttpClient(ConfigurationService.ServerCoreUrl);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
        {
            return AuthResult.ValidationError(validation.Errors, "Please correct the errors before logging in.");
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/tokens", request);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (data != null)
                {
                    return AuthResult.Success(data);
                }
                return AuthResult.Fail("Invalid response from server.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthResult.Fail("Invalid email or password.");
            }

            return AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login failed: {ex.Message}");
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
        {
            return AuthResult.ValidationError(validation.Errors, "Please correct the errors before registering.");
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/users", request);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (data != null)
                {
                    return AuthResult.Success(data);
                }
                return AuthResult.Fail("Invalid response from server.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return AuthResult.Fail("Email is already registered.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var errors = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(content);
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

            return AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Registration failed: {ex.Message}");
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }

    public async Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/users/verify-email", request);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (data != null)
                {
                    return AuthResult.Success(data);
                }
                return AuthResult.Fail("Invalid response from server.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return AuthResult.Fail("Invalid PIN.");
            }

            return AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Verification failed: {ex.Message}");
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }
}
