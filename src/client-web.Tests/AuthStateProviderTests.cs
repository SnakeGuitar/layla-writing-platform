using System.Text;
using System.Text.Json;
using client_web.Application.Services.Auth;
using ClientWeb.Tests.Fakes;
using Xunit;

namespace ClientWeb.Tests;

public class AuthStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsAnonymous_WhenTokenIsMissing()
    {
        var provider = new LaylaAuthenticationStateProvider(new FakeSessionManager());

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ProjectsNameAndRoleClaims()
    {
        var session = new FakeSessionManager
        {
            CurrentToken = CreateJwt(new Dictionary<string, object>
            {
                ["name"] = "Ada",
                ["role"] = "Admin"
            }),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        var provider = new LaylaAuthenticationStateProvider(session);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("Ada", state.User.Identity?.Name);
        Assert.True(state.User.IsInRole("Admin"));
    }

    private static string CreateJwt(Dictionary<string, object> payload)
    {
        var header = new Dictionary<string, object> { ["alg"] = "none", ["typ"] = "JWT" };
        return $"{Base64Url(header)}.{Base64Url(payload)}.";
    }

    private static string Base64Url(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
