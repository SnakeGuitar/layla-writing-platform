namespace Layla.Core.Configuration;

public class PayPalSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Currency { get; set; } = "MXN";
}
