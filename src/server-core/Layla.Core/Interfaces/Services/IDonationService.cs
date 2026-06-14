using Layla.Core.Common;
using Layla.Core.Contracts.Donation;

namespace Layla.Core.Interfaces.Services;

public interface IDonationService
{
    Task<Result<PayPalDonationOrderResponseDto>> CreatePayPalOrderAsync(Guid projectId, CreatePayPalDonationOrderRequestDto request, string donorUserId, CancellationToken cancellationToken = default);
    Task<Result<DonationResponseDto>> CapturePayPalOrderAsync(Guid projectId, CapturePayPalDonationRequestDto request, string donorUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<DonationResponseDto>>> GetProjectDonationsAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task<Result<DonationSummaryResponseDto>> GetProjectDonationSummaryAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
}
