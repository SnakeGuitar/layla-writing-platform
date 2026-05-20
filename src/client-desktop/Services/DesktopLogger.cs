using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Layla.Desktop.Services
{
    public sealed class DesktopLoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;

        public DesktopLoggerProvider()
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Layla", "logs");
            try
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
            catch
            {
                // Fallback to current directory if LocalAppData is inaccessible
                logDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            _logFilePath = Path.Combine(logDir, "desktop.log");
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DesktopLogger(categoryName, _logFilePath);
        }

        public void Dispose() { }
    }

    public sealed class DesktopLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private static readonly object _lock = new object();

        public DesktopLogger(string categoryName, string logFilePath)
        {
            _categoryName = categoryName;
            _logFilePath = logFilePath;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var exceptionString = exception != null ? $"{Environment.NewLine}{exception}" : string.Empty;
            var logLine = $"[{timestamp}] [{logLevel}] [{_categoryName}] {message}{exceptionString}";

            // 1. Write to System.Diagnostics.Debug for developer diagnostic outputs in the IDE
            System.Diagnostics.Debug.WriteLine(logLine);

            // 2. Write to local file for production audit and offline diagnostic stability
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
                }
            }
            catch
            {
                // Silent catch: logger must never crash the hosting application under any I/O failure
            }
        }
    }
}
