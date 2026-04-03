using System.Text;
using System.Text.Json;
using Polly;

namespace client_web.Services.Http;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiClient(HttpClient httpClient, string baseUrl, IAsyncPolicy<HttpResponseMessage> retryPolicy)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _retryPolicy = retryPolicy;
    }

    public async Task<T> RequestAsync<T>(RequestHttp request)
    {
        try
        {
            using var httpRequest = BuildHttpRequest(request);
            using var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(httpRequest));

            var rawText = await response.Content.ReadAsStringAsync();

            object? data;
            try
            {
                data = JsonSerializer.Deserialize<JsonElement>(rawText, JsonOptions);
            }
            catch
            {
                data = rawText;
            }

            if (!response.IsSuccessStatusCode)
            {
                var mensaje = TryExtractMessage(rawText);
                throw new ApiException(
                    mensaje ?? "Error en la solicitud",
                    (int)response.StatusCode,
                    responseData: data
                );
            }

            return JsonSerializer.Deserialize<T>(rawText, JsonOptions)
                ?? throw new ApiException("Respuesta vacía del servidor", (int)response.StatusCode);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApiException("Error de conexión con el servidor", 0, ex.Message);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private HttpRequestMessage BuildHttpRequest(RequestHttp request)
    {
        var url = $"{_baseUrl}{request.Endpoint}";
        var httpRequest = new HttpRequestMessage(request.Method, url);

        // Headers personalizados
        if (request.Headers is not null)
            foreach (var (key, value) in request.Headers)
                httpRequest.Headers.TryAddWithoutValidation(key, value);

        // Bearer token
        if (!string.IsNullOrEmpty(request.Token))
            httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {request.Token}");

        // Body
        if (request.Body is not null)
        {
            var json = JsonSerializer.Serialize(request.Body, JsonOptions);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return httpRequest;
    }

    private static string? TryExtractMessage(string rawText)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawText);
            var root = doc.RootElement;

            if (root.TryGetProperty("mensaje", out var m)) return m.GetString();
            if (root.TryGetProperty("error", out var e)) return e.GetString();
        }
        catch { /* no es JSON */ }

        return null;
    }
}