namespace client_web.Config;

public static class Secrets
{
    static WebApplicationBuilder? _builder;

    public static void Configure(WebApplicationBuilder builder)
    {
        _builder = builder;
        _ = RequireConfig("Ports:HTTPS");
        _ = RequireConfig("Ports:HTTP");
    }

    public static string RequireConfig(string key)
    {
        string? value = _builder?.Configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing required configuration '{key}'. Set it via environment variable or user-secrets.");
        return value;
    }
}