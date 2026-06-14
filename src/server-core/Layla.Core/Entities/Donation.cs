namespace Layla.Core.Entities;

public class Donation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string DonorUserId { get; set; } = string.Empty;
    public AppUser DonorUser { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PayPalOrderId { get; set; } = string.Empty;
    public string? PayPalCaptureId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CapturedAt { get; set; }
}
