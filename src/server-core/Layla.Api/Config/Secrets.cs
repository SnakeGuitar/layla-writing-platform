namespace Layla.Api.Config;

public static class Secrets
{
    const int MinJwtSecretLength = 32;
    public static void Configure(WebApplicationBuilder builder)
    {
        var jwtSecret = RequireConfig(builder, "JwtSettings:Secret");
        if (jwtSecret.Length < MinJwtSecretLength)
            throw new InvalidOperationException(
                $"'JwtSettings:Secret' must be at least {MinJwtSecretLength} characters for HS256 security.");

        _ = RequireConfig(builder, "ConnectionStrings:DefaultConnection");
        _ = RequireConfig(builder, "RabbitMQ:UserName");
        _ = RequireConfig(builder, "RabbitMQ:Password");
    }

    public static string RequireConfig(WebApplicationBuilder builder, string key)
    {
        var value = builder.Configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing required configuration '{key}'. Set it via environment variable or user-secrets.");
        return value;
    }
}