using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using client_web.Application.Services.Session;

namespace client_web.Application.Config.Http;

public class WorldbuildingClient
{
    private readonly HttpClient _http;
    private readonly ILogger<WorldbuildingClient> _logger;
    private readonly ISessionManager _session;

    public WorldbuildingClient(HttpClient http, ILogger<WorldbuildingClient> logger, ISessionManager session)
    {
        _http = http;
        _logger = logger;
        _session = session;
    }

    public async Task<T> SendAsync<T>(APIRequest request, CancellationToken ct = default)
    {
        using var httpRequest = BuildHttpRequest(request);

        HttpResponseMessage response;

        try
        {
            response = await _http.SendAsync(httpRequest, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new APIException("Timeout", 0, null, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new APIException("Error de red", 0, null, ex);
        }

        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _session.ClearAsync();
            }

            throw new APIException(
                ExtractErrorMessage(raw) ?? $"HTTP {(int)response.StatusCode}",
                (int)response.StatusCode,
                raw
            );
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(raw))
        {
            return default!;
        }

        if (response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            throw new APIException("Respuesta no es JSON", (int)response.StatusCode, raw);
        }

        try
        {
            var data = JsonSerializer.Deserialize<T>(raw, JsonOptions);
            return data
                ?? throw new APIException("Respuesta sin datos", (int)response.StatusCode);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializando respuesta de {Endpoint}", request.Endpoint);
            throw new APIException("Respuesta inválida", (int)response.StatusCode, raw);
        }
    }

    // Notice we ignore nulls here, required by Node.js worldbuilding Zod schemas (.optional)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static string? ExtractErrorMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String) return root.GetString();
            foreach (var key in new[] { "message", "title", "error", "detail" })
            {
                if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
            }
        }
        catch { }
        return null;
    }

    private HttpRequestMessage BuildHttpRequest(APIRequest request)
    {
        var httpRequest = new HttpRequestMessage(request.Method, request.Endpoint);

        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!string.IsNullOrEmpty(request.Token))
        {
            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", request.Token);
        }

        if (request.Body != null)
        {
            var json = JsonSerializer.Serialize(request.Body, JsonOptions);
            httpRequest.Content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
        }

        return httpRequest;
    }
}
