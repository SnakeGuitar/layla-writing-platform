using System.Net.Http;
using System.Security.Authentication;

namespace Layla.Desktop.Services;

public static class ConfigurationService
{
    /// <summary>
    /// Base URL for the .NET Server Core API (Identity, Projects, Users).
    /// </summary>
    public static string SERVER_CORE_URL { get; } = "https://localhost:7166";

    /// <summary>
    /// Base URL for the Node.js Worldbuilding API (Manuscripts, Wiki, Graph).
    /// </summary>
    public static string WORLDBUILDING_API_URL { get; } = "http://localhost:3000";

    public static HttpClient CreateHttpClient(string baseUrl)
    {
        HttpClientHandler? handler = new()
        {
            // TODO-Produccion: Solo para desarrollo con certificados autofirmados
            ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true,
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };

        HttpClient client = new(handler)
        {
            BaseAddress = new(baseUrl)
        };

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new("application/json"));
        return client;
    }
}
