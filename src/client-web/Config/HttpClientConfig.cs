using client_web.Services.Http;
using Polly;

namespace client_web.Config;

public static class HttpClientConfig
{
    public static void Configure(IServiceCollection services, WebApplicationBuilder builder)
    {
        string? backendUrl = builder.Configuration["ApiUrls:BackendURL"]
            ?? throw new InvalidOperationException("Falta la configuración ApiUrls:BackendURL");

        // Política de retry con backoff exponencial
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r =>
                !r.IsSuccessStatusCode &&
                ((int)r.StatusCode >= 500 ||
                 r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                 r.StatusCode == System.Net.HttpStatusCode.TooManyRequests))
            .WaitAndRetryAsync(3, attempt =>
                TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));

        // Cliente base para servicios que necesitan la URL base (SignalR, etc.)
        services.AddHttpClient("Backend", client =>
        {
            client.BaseAddress = new Uri(backendUrl);
        });

        // ApiClient con retry policy
        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("Backend");
            return new ApiClient(httpClient, backendUrl, retryPolicy);
        });
    }
}
