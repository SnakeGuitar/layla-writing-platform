namespace Layla.Core.Contracts.Donation;

public class PayPalDonationOrderResponseDto
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}
