namespace QueueManager.Config;

public static class Secrets
{
    static WebApplicationBuilder? _builder;

    public static void Configure(WebApplicationBuilder builder)
    {
        _builder = builder;
        _ = RequireConfig("RabbitMQ:HostName");
        _ = RequireConfig("RabbitMQ:Port");
        _ = RequireConfig("RabbitMQ:UserName");
        _ = RequireConfig("RabbitMQ:Password");
        _ = RequireConfig("RabbitMQ:VirtualHost");
        _ = RequireConfig("RabbitMQ:ExchangeName");
        _ = RequireConfig("RabbitMQ:QueueName");
    }

    public static string RequireConfig(string key)
    {
        string? value = _builder?.Configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing required configuration '{key}'.\n" +
                "Set it via environment variable or user-secrets.");
        return value;
    }
}