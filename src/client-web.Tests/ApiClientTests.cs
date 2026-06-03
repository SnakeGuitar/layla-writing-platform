using System.Net;
using client_web.Application.Config.Http;
using ClientWeb.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClientWeb.Tests;

public class ApiClientTests
{
    [Fact]
    public async Task SendAsync_ClearsSessionAndThrows_OnUnauthorized()
    {
        var session = new FakeSessionManager { CurrentToken = "token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        var client = CreateClient(session, _ => StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, """{"message":"No autorizado"}"""));

        var ex = await Assert.ThrowsAsync<APIException>(() => client.SendAsync<object>(new APIRequest
        {
            Endpoint = "/api/projects",
            Method = HttpMethod.Get,
            Token = "token"
        }));

        Assert.Equal(401, ex.Status);
        Assert.True(session.WasCleared);
    }

    [Fact]
    public async Task SendAsync_DeserializesCamelCaseJson()
    {
        var client = CreateClient(new FakeSessionManager(), _ => StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"displayName":"Ada"}"""));

        var result = await client.SendAsync<TestDto>(new APIRequest
        {
            Endpoint = "/api/users/me",
            Method = HttpMethod.Get
        });

        Assert.Equal("Ada", result.DisplayName);
    }

    [Fact]
    public async Task SendAsync_ThrowsApiException_OnInvalidJson()
    {
        var client = CreateClient(new FakeSessionManager(), _ => StubHttpMessageHandler.Json(HttpStatusCode.OK, "{"));

        var ex = await Assert.ThrowsAsync<APIException>(() => client.SendAsync<TestDto>(new APIRequest
        {
            Endpoint = "/api/users/me",
            Method = HttpMethod.Get
        }));

        Assert.Equal(200, ex.Status);
    }

    private static ApiClient CreateClient(FakeSessionManager session, Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var http = new HttpClient(new StubHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://localhost")
        };
        return new ApiClient(http, NullLogger<ApiClient>.Instance, session);
    }

    private sealed class TestDto
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
