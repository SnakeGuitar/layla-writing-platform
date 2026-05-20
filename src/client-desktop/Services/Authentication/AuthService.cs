using Layla.Desktop.Models.Authentication;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Layla.Desktop.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ILogger<AuthService> logger)
        {
            _logger = logger;
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

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return AuthResult.Fail("Invalid email or password.");
                }

                return AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
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

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return AuthResult.Fail("Email is already registered.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
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
                _logger.LogError(ex, "Registration failed");
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

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return AuthResult.Fail("Invalid PIN.");
                }

                return AuthResult.Fail("The service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification failed");
                return AuthResult.Fail("Network error. Could not connect to the server.");
            }
        }
    }
}
