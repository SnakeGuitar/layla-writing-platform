using System.Net;

namespace ClientWeb.Tests.Fakes;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        => new(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
}
