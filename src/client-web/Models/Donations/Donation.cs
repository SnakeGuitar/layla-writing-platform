namespace client_web.Models.Donations;

public class Donation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string DonorUserId { get; set; } = string.Empty;
    public string DonorDisplayName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PayPalOrderId { get; set; } = string.Empty;
    public string? PayPalCaptureId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
}
