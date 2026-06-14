using client_web.Models.Donations;

namespace client_web.Application.Services.Donations;

public interface IDonationService
{
    Task<DonationActionResult<PayPalDonationOrder>> CreatePayPalOrderAsync(Guid projectId, decimal amount);
    Task<DonationActionResult<Donation>> CapturePayPalOrderAsync(Guid projectId, string orderId);
    Task<IReadOnlyList<Donation>> GetProjectDonationsAsync(Guid projectId);
    Task<DonationSummary?> GetProjectDonationSummaryAsync(Guid projectId);
}
