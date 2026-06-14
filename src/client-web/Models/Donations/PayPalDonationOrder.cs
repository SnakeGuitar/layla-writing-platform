namespace client_web.Models.Donations;

public class PayPalDonationOrder
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}
