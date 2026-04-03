using System.Text;
using System.Text.Json;
using Polly;

namespace client_web.Services.Http;

public class ApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<T> RequestAsync<T>(RequestHttp<T> request, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = BuildHttpRequest(request);
            using var response = await _httpClient.SendAsync(httpRequest, ct);

            string rawText = await response.Content.ReadAsStringAsync();

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
        catch (HttpRequestException ex)
        {
            throw new ApiException("Error de red", 0, null, ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException("Timeout", 0, null, ex);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private HttpRequestMessage BuildHttpRequest<T>(RequestHttp<T> request)
    {
        var httpRequest = new HttpRequestMessage(request.Method, request.Endpoint);

        // Headers personalizados
        if (request.Headers is not null)
            foreach (var header in request.Headers)
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

        // Bearer token
        if (!string.IsNullOrEmpty(request.Token))
            httpRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.Token);

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