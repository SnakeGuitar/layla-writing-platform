namespace client_web.Models.Donations;

public class DonationSummary
{
    public Guid ProjectId { get; set; }
    public decimal TotalAmount { get; set; }
    public int DonationCount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? LastDonationAt { get; set; }
}
