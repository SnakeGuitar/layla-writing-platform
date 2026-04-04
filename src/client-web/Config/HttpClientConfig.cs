using client_web.Application.Services.Http;
using Polly;

namespace client_web.Config;

public static class HttpClientConfig
{
    public static void Configure(IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddHttpClient();
        // Política de retry con backoff exponencial
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r =>
                !r.IsSuccessStatusCode &&
                ((int)r.StatusCode >= 500 ||
                 r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                 r.StatusCode == System.Net.HttpStatusCode.TooManyRequests))
            .WaitAndRetryAsync(3, attempt =>
                TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));

        string backendUrl = builder.Configuration["ApiUrls:BackendURL"] ?? throw new InvalidOperationException("ApiUrls:BackendURL no configurado");

        // Typed client con retry policy aplicada directamente
        services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri(backendUrl);
        })
        .AddPolicyHandler(retryPolicy);
    }
}
