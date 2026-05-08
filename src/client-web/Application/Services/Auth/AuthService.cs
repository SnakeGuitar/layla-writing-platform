using System.Net;
using System.Text.Json;
using client_web.Application.Config.Http;
using client_web.Application.Schemas.Auth;
using client_web.Models.Authentication;

namespace client_web.Application.Services.Auth;

/// <summary>
/// Adapter on top of <see cref="ApiClient"/> that talks to server-core's
/// authentication endpoints and projects the response into an
/// <see cref="AuthResult"/>. Mirrors the structure of the desktop client's
/// <c>Layla.Desktop.Services.AuthService</c>.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApiClient _client;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApiClient client, ILogger<AuthService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
            return AuthResult.ValidationError(validation.Errors, "Please correct the errors before logging in.");

        try
        {
            var response = await _client.SendAsync<LoginResponse>(new APIRequest
            {
                Endpoint = "/api/tokens",
                Method = HttpMethod.Post,
                Body = request,
            });
            return AuthResult.Success(response);
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.Unauthorized)
        {
            return AuthResult.Fail("Invalid email or password.");
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.Locked)
        {
            return AuthResult.Fail("Your account is temporarily locked. Try again later.");
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Login failed for {Email}", request.Email);
            return AuthResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }

    /// <inheritdoc/>
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
            return AuthResult.ValidationError(validation.Errors, "Please correct the errors before registering.");

        try
        {
            var response = await _client.SendAsync<LoginResponse>(new APIRequest
            {
                Endpoint = "/api/users",
                Method = HttpMethod.Post,
                Body = request,
            });
            return AuthResult.Success(response);
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            return AuthResult.Fail("Email is already registered.");
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.BadRequest)
        {
            // server-core returns ValidationProblemDetails on model errors —
            // try to surface per-field messages when present.
            var errors = TryExtractValidationErrors(ex.ResponseData);
            if (errors is not null && errors.Count > 0)
                return AuthResult.ValidationError(errors, "Validation failed. Please check your input.");
            return AuthResult.Fail(ex.Message);
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Email}", request.Email);
            return AuthResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }

    /// <inheritdoc/>
    public async Task<AuthResult> VerifyEmailAsync(VerifyEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Pin))
            return AuthResult.Fail("Email and PIN are required.");

        try
        {
            var response = await _client.SendAsync<LoginResponse>(new APIRequest
            {
                Endpoint = "/api/users/verify-email",
                Method = HttpMethod.Post,
                Body = request,
            });
            return AuthResult.Success(response);
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.BadRequest)
        {
            return AuthResult.Fail("Invalid PIN.");
        }
        catch (APIException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return AuthResult.Fail("User not found. Please register again.");
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "Email verification failed for {Email}", request.Email);
            return AuthResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email verification for {Email}", request.Email);
            return AuthResult.Fail("Network error. Could not connect to the server.");
        }
    }

    private static Dictionary<string, List<string>>? TryExtractValidationErrors(object? raw)
    {
        if (raw is not string body || string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, List<string>>();
                foreach (var prop in errors.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.Array) continue;
                    var messages = prop.Value.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .ToList();
                    if (messages.Count > 0) dict[prop.Name] = messages;
                }
                return dict;
            }
        }
        catch
        {
            // Body wasn't ProblemDetails JSON — caller falls through.
        }
        return null;
    }
}
