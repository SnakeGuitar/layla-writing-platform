using System.Text;
using System.Text.Json;

namespace client_web.Application.Services.Http;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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

        if (response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            throw new APIException("Respuesta no es JSON", (int)response.StatusCode, raw);
        }

        APIResponse<T>? apiResponse;

        try
        {
            apiResponse = JsonSerializer.Deserialize<APIResponse<T>>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializando respuesta");
            throw new APIException("Respuesta inválida", (int)response.StatusCode, raw);
        }

        if (!response.IsSuccessStatusCode || apiResponse?.IsError == true)
        {
            throw new APIException(
                apiResponse?.Message ?? "Error en la solicitud",
                (int)response.StatusCode,
                apiResponse
            );
        }

        return apiResponse!.Data
            ?? throw new APIException("Respuesta sin datos", (int)response.StatusCode);
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
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.Token);
        }

        if (request.Body != null)
        {
            var json = JsonSerializer.Serialize(request.Body, JsonOptions);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return httpRequest;
    }
}