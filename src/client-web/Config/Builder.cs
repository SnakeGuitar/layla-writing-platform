namespace client_web.Config;

public static class Builder
{
    public static void Configure(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddHealthChecks();
        services.AddRazorComponents().AddInteractiveServerComponents();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(); // Esto imprime en consola
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.WebHost.UseUrls(
            $"https://localhost:{RequireConfig(builder, "WEB_PORT_HTTPS")};http://localhost:{RequireConfig(builder, "WEB_PORT_HTTP")};");

    }

    public static string RequireConfig(WebApplicationBuilder builder, string key)
    {
        var value = builder.Configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing required configuration '{key}'. Set it via environment variable or user-secrets.");
        return value;
    }
}