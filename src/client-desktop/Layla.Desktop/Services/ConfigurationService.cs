using System;

namespace Layla.Desktop.Services
{
    public static class ConfigurationService
    {
        /// <summary>
        /// Base URL for the .NET Server Core API (Identity, Projects, Users).
        /// </summary>
        public static string ServerCoreUrl { get; } = "https://localhost:7165";

        /// <summary>
        /// Base URL for the Node.js Worldbuilding API (Manuscripts, Wiki, Graph).
        /// </summary>
        public static string WorldbuildingApiUrl { get; } = "http://localhost:3000";
    }
}
