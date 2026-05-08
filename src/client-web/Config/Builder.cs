namespace client_web.Config;

public static class Builder
{
    public static void Configure(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddHealthChecks();
        services.AddRazorComponents().AddInteractiveServerComponents();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.WebHost.UseUrls(
            $"https://localhost:{builder.Configuration["Ports:HTTPS"]};http://localhost:{builder.Configuration["Ports:HTTP"]};");

    }
}