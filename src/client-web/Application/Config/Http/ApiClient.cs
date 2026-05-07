using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace client_web.Application.Config.Http;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, ILogger<ApiClient> logger)
    {
        _http = http;
        _logger = logger;
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

        // server-core returns DTOs directly (`return Ok(result.Data)`), not wrapped
        // in any envelope. Treat the HTTP status code as the success/error signal
        // and deserialize the body straight into T.
        if (!response.IsSuccessStatusCode)
        {
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

    /// <summary>JSON options matching ASP.NET Core defaults (camelCase, case-insensitive).</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Best-effort extraction of an error message from a server response. Tries
    /// common shapes (ProblemDetails, plain string, anonymous object with a
    /// 'message'/'error'/'title' field) and falls back to null on failure.
    /// </summary>
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
        catch
        {
            // Body was not JSON — fall through.
        }
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
            // Use camelCase + ASP.NET-Core-friendly options so the body shape
            // matches what the controllers expect.
            var json = JsonSerializer.Serialize(request.Body, JsonOptions);
            httpRequest.Content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
        }

        return httpRequest;
    }
}