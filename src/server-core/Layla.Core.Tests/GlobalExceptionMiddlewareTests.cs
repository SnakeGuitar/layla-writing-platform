using System.Text.Json;
using Layla.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Layla.Core.Tests;

// ── next completes normally — response untouched ──────────────────────────────

public class GlobalExceptionMiddleware_WhenNextSucceeds
{
    private readonly HttpContext _ctx;

    public GlobalExceptionMiddleware_WhenNextSucceeds()
    {
        _ctx = new DefaultHttpContext();
        _ctx.Response.Body = new MemoryStream();
        var mw = new GlobalExceptionMiddleware(
            _ => Task.CompletedTask,
            NullLogger<GlobalExceptionMiddleware>.Instance);
        mw.InvokeAsync(_ctx).GetAwaiter().GetResult();
    }

    [Fact] public void StatusCodeRemainsDefault() => Assert.Equal(200, _ctx.Response.StatusCode);
}

// ── next throws — status is 500 ───────────────────────────────────────────────

public class GlobalExceptionMiddleware_WhenNextThrows_StatusCode
{
    private readonly HttpContext _ctx;

    public GlobalExceptionMiddleware_WhenNextThrows_StatusCode()
    {
        _ctx = new DefaultHttpContext();
        _ctx.Response.Body = new MemoryStream();
        var mw = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        mw.InvokeAsync(_ctx).GetAwaiter().GetResult();
    }

    [Fact] public void StatusCodeIs500() => Assert.Equal(500, _ctx.Response.StatusCode);
    [Fact] public void ContentTypeIsJson() => Assert.Equal("application/json", _ctx.Response.ContentType);
}

// ── next throws — response body is a valid error object ───────────────────────

public class GlobalExceptionMiddleware_WhenNextThrows_ResponseBody
{
    private readonly JsonElement _body;

    public GlobalExceptionMiddleware_WhenNextThrows_ResponseBody()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        var mw = new GlobalExceptionMiddleware(
            _ => throw new Exception("something went wrong"),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        mw.InvokeAsync(ctx).GetAwaiter().GetResult();

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = new StreamReader(ctx.Response.Body).ReadToEnd();
        _body = JsonDocument.Parse(json).RootElement;
    }

    [Fact] public void BodyHasStatusCode500() => Assert.Equal(500, _body.GetProperty("StatusCode").GetInt32());
    [Fact] public void BodyHasNonEmptyErrorMessage() =>
        Assert.False(string.IsNullOrWhiteSpace(_body.GetProperty("Error").GetString()));
    [Fact] public void BodyHasTraceId() => Assert.True(_body.TryGetProperty("TraceId", out _));
}
