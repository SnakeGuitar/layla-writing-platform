namespace client_web.Config;

public static class Builder
{
    public static void Configure(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(); // Esto es lo que imprime en consola
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
    }
}