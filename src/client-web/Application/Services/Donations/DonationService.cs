using client_web.Application.Config.Http;
using client_web.Application.Services.Session;
using client_web.Models.Donations;

namespace client_web.Application.Services.Donations;

public class DonationService : IDonationService
{
    private readonly ApiClient _client;
    private readonly ISessionManager _session;
    private readonly ILogger<DonationService> _logger;

    public DonationService(ApiClient client, ISessionManager session, ILogger<DonationService> logger)
    {
        _client = client;
        _session = session;
        _logger = logger;
    }

    private string? Token => _session.IsAuthenticated ? _session.CurrentToken : null;

    public async Task<DonationActionResult<PayPalDonationOrder>> CreatePayPalOrderAsync(Guid projectId, decimal amount)
    {
        try
        {
            var data = await _client.SendAsync<PayPalDonationOrder>(new APIRequest
            {
                Endpoint = $"/api/projects/{projectId}/donations/paypal/order",
                Method = HttpMethod.Post,
                Token = Token,
                Body = new CreatePayPalDonationOrderRequest { Amount = amount }
            });

            return DonationActionResult<PayPalDonationOrder>.Success(data);
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "CreatePayPalOrderAsync failed for project {ProjectId}.", projectId);
            return DonationActionResult<PayPalDonationOrder>.Fail(ex.Message);
        }
    }

    public async Task<DonationActionResult<Donation>> CapturePayPalOrderAsync(Guid projectId, string orderId)
    {
        try
        {
            var data = await _client.SendAsync<Donation>(new APIRequest
            {
                Endpoint = $"/api/projects/{projectId}/donations/paypal/capture",
                Method = HttpMethod.Post,
                Token = Token,
                Body = new CapturePayPalDonationRequest { OrderId = orderId }
            });

            return DonationActionResult<Donation>.Success(data);
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "CapturePayPalOrderAsync failed for project {ProjectId}.", projectId);
            return DonationActionResult<Donation>.Fail(ex.Message);
        }
    }

    public async Task<IReadOnlyList<Donation>> GetProjectDonationsAsync(Guid projectId)
    {
        try
        {
            var data = await _client.SendAsync<List<Donation>>(new APIRequest
            {
                Endpoint = $"/api/projects/{projectId}/donations",
                Method = HttpMethod.Get,
                Token = Token
            });

            return data;
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetProjectDonationsAsync failed for project {ProjectId}.", projectId);
            return Array.Empty<Donation>();
        }
    }

    public async Task<DonationSummary?> GetProjectDonationSummaryAsync(Guid projectId)
    {
        try
        {
            return await _client.SendAsync<DonationSummary>(new APIRequest
            {
                Endpoint = $"/api/projects/{projectId}/donations/summary",
                Method = HttpMethod.Get,
                Token = Token
            });
        }
        catch (APIException ex)
        {
            _logger.LogWarning(ex, "GetProjectDonationSummaryAsync failed for project {ProjectId}.", projectId);
            return null;
        }
    }
}
