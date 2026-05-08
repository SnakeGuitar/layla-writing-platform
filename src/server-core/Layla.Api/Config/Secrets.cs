namespace Layla.Api.Config;

public static class Secrets
{
    static WebApplicationBuilder? _builder;

    public static void Configure(WebApplicationBuilder builder)
    {
        _builder = builder;
        _ = RequireConfig("Ports:HTTPS");
        _ = RequireConfig("Ports:HTTP");

        _ = RequireConfig("JwtSettings:Secret");
        _ = RequireConfig("JwtSettings:Issuer");
        _ = RequireConfig("JwtSettings:Audience");
        _ = RequireConfig("JwtSettings:ExpirationInMinutes");

        _ = RequireConfig("DatabaseConfigs:SQL:ConnectionString");
        _ = RequireConfig("DatabaseConfigs:SQL:CommandTimeoutSeconds");
        _ = RequireConfig("DatabaseConfigs:SQL:MaxRetryCount");
        _ = RequireConfig("DatabaseConfigs:SQL:MaxRetryDelay");

        _ = RequireConfig("RabbitMQ:HostName");
        _ = RequireConfig("RabbitMQ:Port");
        _ = RequireConfig("RabbitMQ:UserName");
        _ = RequireConfig("RabbitMQ:Password");

        _ = RequireConfig("EmailConfigs:Host");
        _ = RequireConfig("EmailConfigs:Port");
        _ = RequireConfig("EmailConfigs:Username");
        _ = RequireConfig("EmailConfigs:Password");
        _ = RequireConfig("EmailConfigs:FromName");
        _ = RequireConfig("EmailConfigs:FromEmail");
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