using System.IO;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;

namespace Layla.Desktop.Services;

/// <summary>
/// Resolves API base URLs at runtime so the desktop client works in every
/// environment without recompilation:
/// <list type="bullet">
///   <item><b>Local dev (host)</b> — reads <c>layla.config.json</c> from
///         <c>%LOCALAPPDATA%\Layla\</c>; falls back to the defaults below.</item>
///   <item><b>Docker</b> — same mechanism; point both URLs at the API Gateway
///         (<c>http://localhost:5000</c>) or use the per-service ports.</item>
/// </list>
/// </summary>
public static class ConfigurationService
{
    // -------------------------------------------------------------------------
    // Defaults: match the local-dev `dotnet run` ports.
    // Docker users override these in %LOCALAPPDATA%\Layla\layla.config.json.
    // -------------------------------------------------------------------------

    /// <summary>Base URL for the .NET Server Core API (Identity, Projects, Users).</summary>
    public static string SERVER_CORE_URL { get; private set; } = "https://localhost:7166";

    /// <summary>Base URL for the Node.js Worldbuilding API (Manuscripts, Wiki, Graph).</summary>
    public static string WORLDBUILDING_API_URL { get; private set; } = "http://localhost:3000";

    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Layla");

    private static readonly string ConfigFile =
        Path.Combine(ConfigDir, "layla.config.json");

    /// <summary>
    /// Called once from <see cref="App.OnStartup"/> before any service is resolved.
    /// Reads <c>layla.config.json</c> and overrides the defaults if the file exists.
    /// </summary>
    public static void Load()
    {
        try
        {
            if (!File.Exists(ConfigFile)) return;

            string json = File.ReadAllText(ConfigFile);
            JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("ServerCoreUrl", out JsonElement coreUrl) &&
                !string.IsNullOrWhiteSpace(coreUrl.GetString()))
                SERVER_CORE_URL = coreUrl.GetString()!.TrimEnd('/');

            if (root.TryGetProperty("WorldbuildingUrl", out JsonElement wbUrl) &&
                !string.IsNullOrWhiteSpace(wbUrl.GetString()))
                WORLDBUILDING_API_URL = wbUrl.GetString()!.TrimEnd('/');

            System.Diagnostics.Debug.WriteLine(
                $"[ConfigurationService] Loaded from file — Core: {SERVER_CORE_URL}  WB: {WORLDBUILDING_API_URL}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Load failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes (or overwrites) <c>layla.config.json</c> with the supplied URLs.
    /// Called from the Settings view when the user changes the connection profile.
    /// </summary>
    public static void Save(string serverCoreUrl, string worldbuildingUrl)
    {
        try
        {
            if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
            var obj = new { ServerCoreUrl = serverCoreUrl, WorldbuildingUrl = worldbuildingUrl };
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
            SERVER_CORE_URL = serverCoreUrl.TrimEnd('/');
            WORLDBUILDING_API_URL = worldbuildingUrl.TrimEnd('/');
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Emits a <see cref="layla.config.json"/> template to the config directory
    /// if the file does not already exist, so users can discover and edit it.
    /// </summary>
    public static void EnsureDefaultConfigFile()
    {
        try
        {
            if (File.Exists(ConfigFile)) return;
            if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);

            var template = new
            {
                ServerCoreUrl = SERVER_CORE_URL,
                WorldbuildingUrl = WORLDBUILDING_API_URL,
                _comment = "Change these URLs to match your environment. " +
                           "For Docker use https://localhost:5288 (core) and http://localhost:3000 (worldbuilding), " +
                           "or point both to the API Gateway at http://localhost:5000."
            };
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-critical */ }
    }

    public static HttpClient CreateHttpClient(string baseUrl)
    {
        HttpClientHandler handler = new()
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
