using Layla.Core.Common;
using Layla.Core.Configuration;
using Layla.Core.Constants;
using Layla.Core.Contracts.Donation;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Layla.Core.Services;

public class DonationService : BaseService<DonationService>, IDonationService
{
    private readonly IDonationRepository _donationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPayPalClient _payPalClient;
    private readonly PayPalSettings _settings;

    public DonationService(
        IDonationRepository donationRepository,
        IProjectRepository projectRepository,
        IPayPalClient payPalClient,
        IOptions<PayPalSettings> settings,
        ILogger<DonationService> logger)
        : base(logger)
    {
        _donationRepository = donationRepository;
        _projectRepository = projectRepository;
        _payPalClient = payPalClient;
        _settings = settings.Value;
    }

    public Task<Result<PayPalDonationOrderResponseDto>> CreatePayPalOrderAsync(Guid projectId, CreatePayPalDonationOrderRequestDto request, string donorUserId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            if (request.Amount <= 0)
                return Result<PayPalDonationOrderResponseDto>.Failure(ErrorCode.InvalidInput, "Donation amount must be greater than zero.");

            var project = await _projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
            if (project == null)
                return Result<PayPalDonationOrderResponseDto>.Failure(ErrorCode.ProjectNotFound);

            if (!project.IsPublic)
                return Result<PayPalDonationOrderResponseDto>.Failure(ErrorCode.InvalidInput, "Donations are only available for public projects.");

            var currency = NormalizeCurrency(_settings.Currency);
            var order = await _payPalClient.CreateOrderAsync(request.Amount, currency, $"Layla donation for {project.Title}", cancellationToken);

            var donation = new Donation
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                DonorUserId = donorUserId,
                Amount = decimal.Round(request.Amount, 2),
                Currency = currency,
                Status = DonationStatuses.Created,
                PayPalOrderId = order.OrderId,
                CreatedAt = DateTime.UtcNow
            };

            await _donationRepository.AddDonationAsync(donation, cancellationToken);
            await _donationRepository.SaveChangesAsync(cancellationToken);

            return Result<PayPalDonationOrderResponseDto>.Success(new PayPalDonationOrderResponseDto
            {
                OrderId = order.OrderId,
                Amount = donation.Amount,
                Currency = donation.Currency,
                ClientId = _settings.ClientId
            });
        }, "Failed to create PayPal donation order for project {ProjectId}", projectId);

    public Task<Result<DonationResponseDto>> CapturePayPalOrderAsync(Guid projectId, CapturePayPalDonationRequestDto request, string donorUserId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.OrderId))
                return Result<DonationResponseDto>.Failure(ErrorCode.InvalidInput, "PayPal order ID is required.");

            var donation = await _donationRepository.GetDonationByPayPalOrderIdAsync(request.OrderId, cancellationToken);
            if (donation == null || donation.ProjectId != projectId || donation.DonorUserId != donorUserId)
                return Result<DonationResponseDto>.Failure(ErrorCode.NotFound);

            var capture = await _payPalClient.CaptureOrderAsync(request.OrderId, cancellationToken);
            donation.Status = string.Equals(capture.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                ? DonationStatuses.Captured
                : DonationStatuses.Failed;
            donation.PayPalCaptureId = capture.CaptureId;
            donation.CapturedAt = donation.Status == DonationStatuses.Captured ? DateTime.UtcNow : null;

            await _donationRepository.SaveChangesAsync(cancellationToken);

            if (donation.Status != DonationStatuses.Captured)
                return Result<DonationResponseDto>.Failure(ErrorCode.InvalidInput, "PayPal did not complete the payment capture.");

            return Result<DonationResponseDto>.Success(MapToDto(donation));
        }, "Failed to capture PayPal donation order {OrderId}", request.OrderId);

    public Task<Result<IEnumerable<DonationResponseDto>>> GetProjectDonationsAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var isOwner = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!isOwner)
                return Result<IEnumerable<DonationResponseDto>>.Failure(ErrorCode.Forbidden);

            var donations = await _donationRepository.GetProjectDonationsAsync(projectId, cancellationToken);
            return Result<IEnumerable<DonationResponseDto>>.Success(donations.Select(MapToDto).ToList());
        }, "Failed to get donations for project {ProjectId}", projectId);

    public Task<Result<DonationSummaryResponseDto>> GetProjectDonationSummaryAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var isOwner = await _projectRepository.UserHasRoleInProjectAsync(projectId, userId, ProjectRoles.Owner, cancellationToken);
            if (!isOwner)
                return Result<DonationSummaryResponseDto>.Failure(ErrorCode.Forbidden);

            var donations = (await _donationRepository.GetProjectDonationsAsync(projectId, cancellationToken))
                .Where(d => d.Status == DonationStatuses.Captured)
                .ToList();
            var currency = donations.FirstOrDefault()?.Currency ?? NormalizeCurrency(_settings.Currency);

            return Result<DonationSummaryResponseDto>.Success(new DonationSummaryResponseDto
            {
                ProjectId = projectId,
                TotalAmount = donations.Sum(d => d.Amount),
                DonationCount = donations.Count,
                Currency = currency,
                LastDonationAt = donations.MaxBy(d => d.CapturedAt)?.CapturedAt
            });
        }, "Failed to get donation summary for project {ProjectId}", projectId);

    private static string NormalizeCurrency(string currency) =>
        string.IsNullOrWhiteSpace(currency) ? "MXN" : currency.Trim().ToUpperInvariant();

    private static DonationResponseDto MapToDto(Donation donation) => new()
    {
        Id = donation.Id,
        ProjectId = donation.ProjectId,
        DonorUserId = donation.DonorUserId,
        DonorDisplayName = donation.DonorUser?.DisplayName ?? donation.DonorUser?.Email ?? "Usuario Layla",
        Amount = donation.Amount,
        Currency = donation.Currency,
        Status = donation.Status,
        PayPalOrderId = donation.PayPalOrderId,
        PayPalCaptureId = donation.PayPalCaptureId,
        CreatedAt = donation.CreatedAt,
        CapturedAt = donation.CapturedAt
    };
}
