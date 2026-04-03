using client_web.Services.Http;

namespace client_web.Config;

public static class Secure
{
    public static void Configure(this IServiceCollection services, WebApplicationBuilder builder)
    {
        string? apiAccesoUrl = builder.Configuration["ApiUrls:Acceso"] ?? throw new InvalidOperationException("Falta la configuración ApiUrls:Acceso");

        services.AddHttpClient<ApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(apiAccesoUrl);
        })
        .AddTypedClient((httpClient, sp) => new ApiClient(httpClient, apiAccesoUrl));
    }
}