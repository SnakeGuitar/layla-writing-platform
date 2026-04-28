using client_web.Application.Schemas.Auth;
using client_web.Application.Config.Http;
using client_web.Application.Services.Auth;

namespace client_web.Services;

public class AuthService : IAuthService
{
    private readonly ApiClient _client;

    public AuthService(ApiClient client)
    {
        _client = client;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest requestData)
    {
        return await _client.SendAsync<LoginResponse>(new APIRequest
        {
            Endpoint = "/auth/login",
            Method = HttpMethod.Post,
            Body = requestData
        });
    }
}