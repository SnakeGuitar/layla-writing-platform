using Microsoft.Extensions.Logging;

namespace Layla.Desktop.Services.Logger;

public static class Log
{
    private static readonly ILoggerFactory _factory = LoggerFactory.Create(builder =>
    {
        builder.
            SetMinimumLevel(LogLevel.Trace).
            AddConsole().
            AddDebug();
    });

    public static ILogger<T> For<T>() => _factory.CreateLogger<T>();
}
