using Microsoft.Extensions.Logging;

namespace Umea.se.Toolkit.Logging.ConsoleLogger.Models;

internal readonly struct LogEvent
{
    public long TicksUtc { get; init; }
    public LogLevel Level { get; init; }
    public string Category { get; init; }
    public string Message { get; init; }
    public Exception? Exception { get; init; }
}
