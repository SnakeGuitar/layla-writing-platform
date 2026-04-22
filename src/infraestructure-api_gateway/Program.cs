using ApiGateway.Middlewares;
using ApiGateway.Policies;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Transforms;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// ── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{// TODO
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://___.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// ── RATE LIMITER ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("default", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 100;
        o.QueueLimit = 10;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// ── HEALTH CHECK POLICY ──────────────────────────────────────────────────────
builder.Services.AddSingleton<IActiveHealthCheckPolicy, MinReplicasActivePolicy>();
builder.Services.AddSingleton<IPassiveHealthCheckPolicy, MinReplicasPassivePolicy>();

// ── YARP ─────────────────────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(ctx =>
    {
        ctx.RequestTransforms.Add(new CorrelationIdTransform());
        ctx.AddRequestTransform(reqCtx =>
        {
            reqCtx.ProxyRequest.Headers.TryAddWithoutValidation(
                "X-Gateway-Timestamp",
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            return ValueTask.CompletedTask;
        });
    });


// ── JWT ──────────────────────────────────────────────────────────────────────
/*
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = "api-gateway";
    });
*/

// ──  Exposition of ports ─────────────────────────────────────────────────────
// TODO
builder.WebHost.UseUrls(
    $"https://+:{builder.Configuration["Ports:HTTPS"]};http://+:{builder.Configuration["Ports:HTTP"]};");

WebApplication? app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseRateLimiter();
});
app.Run();