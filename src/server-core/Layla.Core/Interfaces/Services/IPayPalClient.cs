namespace Layla.Core.Interfaces.Services;

public interface IPayPalClient
{
    Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string currency, string description, CancellationToken cancellationToken = default);
    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default);
}

public record PayPalOrderResult(string OrderId, string Status);

public record PayPalCaptureResult(string OrderId, string Status, string? CaptureId);
