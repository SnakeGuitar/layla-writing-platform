using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Layla.Core.Configuration;
using Layla.Core.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace Layla.Infrastructure.Services;

public class PayPalClient : IPayPalClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly PayPalSettings _settings;

    public PayPalClient(HttpClient httpClient, IOptions<PayPalSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string currency, string description, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    description = TrimDescription(description),
                    amount = new
                    {
                        currency_code = currency,
                        value = decimal.Round(amount, 2).ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<PayPalOrderResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("PayPal returned an empty create-order response.");

        if (string.IsNullOrWhiteSpace(order.Id))
            throw new InvalidOperationException("PayPal did not return an order ID.");

        return new PayPalOrderResult(order.Id, order.Status);
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var status = root.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString() ?? string.Empty
            : string.Empty;
        var captureId = TryGetCaptureId(root);

        return new PayPalCaptureResult(orderId, status, captureId);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<PayPalTokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("PayPal returned an empty token response.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("PayPal did not return an access token.");

        return token.AccessToken;
    }

    private static string TrimDescription(string description) =>
        description.Length <= 127 ? description : description[..127];

    private static string? TryGetCaptureId(JsonElement root)
    {
        if (!root.TryGetProperty("purchase_units", out var units) || units.GetArrayLength() == 0)
            return null;

        var unit = units[0];
        if (!unit.TryGetProperty("payments", out var payments) ||
            !payments.TryGetProperty("captures", out var captures) ||
            captures.GetArrayLength() == 0)
        {
            return null;
        }

        return captures[0].TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private sealed class PayPalTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class PayPalOrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
