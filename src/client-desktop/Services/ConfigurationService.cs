using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Layla.Desktop.Services
{
    public static class ConfigurationService
    {
        /// <summary>
        /// Base URL for the .NET Server Core API (Identity, Projects, Users).
        /// </summary>
        public static string ServerCoreUrl { get; } = Environment.GetEnvironmentVariable("LAYLA_GATEWAY_URL") ?? "http://localhost:5000";

        /// <summary>
        /// Base URL for the Node.js Worldbuilding API (Manuscripts, Wiki, Graph).
        /// </summary>
        public static string WorldbuildingApiUrl { get; } = Environment.GetEnvironmentVariable("LAYLA_WORLDBUILDING_URL") ?? "http://localhost:3000";

        public static HttpClient CreateHttpClient(string baseUrl)
        {
            // AuthMessageHandler attaches the bearer per request, avoiding
            // the racy DefaultRequestHeaders.Authorization mutation pattern.
            var client = new HttpClient(new AuthMessageHandler())
            {
                BaseAddress = new Uri(baseUrl)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
