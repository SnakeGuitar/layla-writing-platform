namespace client_web.Config;

public static class Builder
{
    public static void Configure(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddHttpClient("ServerCore", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ApiUrls:Core"] ?? "");
        });
    }
}