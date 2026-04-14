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
        public static string ServerCoreUrl { get; } = "https://localhost:5288";

        /// <summary>
        /// Base URL for the Node.js Worldbuilding API (Manuscripts, Wiki, Graph).
        /// </summary>
        public static string WorldbuildingApiUrl { get; } = "http://localhost:3000";

        public static HttpClient CreateHttpClient(string baseUrl)
        {
            var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
