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
        string bindHost = builder.Environment.IsProduction() ? "+" : "localhost";
        builder.WebHost.UseUrls(
            $"https://{bindHost}:{builder.Configuration["Ports:HTTPS"]};http://{bindHost}:{builder.Configuration["Ports:HTTP"]};");

    }
}