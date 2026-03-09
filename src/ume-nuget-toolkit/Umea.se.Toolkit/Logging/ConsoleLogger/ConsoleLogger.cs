using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Logging.ConsoleLogger.Models;

namespace Umea.se.Toolkit.Logging.ConsoleLogger;

internal sealed class ConsoleLogger : ILogger
{
    private readonly ConsoleLoggerProvider _provider;
    private readonly string _category;

    public ConsoleLogger(ConsoleLoggerProvider provider, string category)
    {
        _provider = provider;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            string message = formatter(state, exception);
            LogEvent logEvent = new()
            {
                TicksUtc = DateTime.UtcNow.Ticks,
                Level = logLevel,
                Category = _category,
                Message = message,
                Exception = exception,
            };
            _provider.Enqueue(logEvent);
        }
    }
}
