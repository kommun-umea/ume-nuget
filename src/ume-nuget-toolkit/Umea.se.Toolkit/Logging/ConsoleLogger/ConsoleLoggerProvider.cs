using System.Globalization;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Logging.ConsoleLogger.Models;

namespace Umea.se.Toolkit.Logging.ConsoleLogger;

[ProviderAlias("Console")]
internal sealed class ConsoleLoggerProvider : ILoggerProvider
{
    private const int _channelCapacity = 10_000;
    private readonly ConsoleColor _defaultColor;
    private string? _lastCategory;

    private readonly Channel<LogEvent> _queue;
    private readonly CancellationTokenSource _stop;

    public ConsoleLoggerProvider()
    {
        _defaultColor = Console.ForegroundColor;

        BoundedChannelOptions channelOptions = new(_channelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        };
        _queue = Channel.CreateBounded<LogEvent>(channelOptions);
        _stop = new CancellationTokenSource();

        Task.Run(WriterLoop);
    }

    public ILogger CreateLogger(string categoryName) => new ConsoleLogger(this, categoryName);

    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _stop.Cancel();
        _stop.Dispose();
    }

    public void Enqueue(in LogEvent logEvent)
    {
        _queue.Writer.TryWrite(logEvent);
    }

    private async Task WriterLoop()
    {
        ChannelReader<LogEvent> reader = _queue.Reader;

        while (await reader.WaitToReadAsync(_stop.Token).ConfigureAwait(false))
        {
            while (reader.TryRead(out LogEvent logEvent))
            {
                Write(logEvent);
            }
        }
    }

    private void Write(in LogEvent logEvent)
    {
        string timestamp = GetLocalTimestamp(logEvent.TicksUtc);
        ConsoleColor levelColor = GetLevelColor(logEvent.Level);
        string logType = logEvent.Level.ToString();
        string message = logEvent.Message;

        if (IsCustomEvent(logEvent))
        {
            levelColor = ConsoleColor.Green;
            logType = "CustomEvent";
            message = message[LoggerExtensions.CustomEventTag.Length..];
        }

        if (!IsLatestCategory(logEvent.Category))
        {
            WriteCategory(logEvent.Category);
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"    {timestamp}");
        Console.ForegroundColor = levelColor;
        Console.Write($" {logType}");
        Console.ResetColor();
        Console.Write(":");

        if (logEvent.Exception is not null)
        {
            Console.Write($" [{logEvent.Exception.GetType().Name}]");
        }

        Console.Write($" {message}");
        Console.WriteLine();
    }

    private static string GetLocalTimestamp(long ticksUtc)
    {
        DateTime localDateTime = new DateTime(ticksUtc, DateTimeKind.Utc).ToLocalTime();
        return localDateTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }

    private ConsoleColor GetLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.DarkCyan,
        LogLevel.Debug => ConsoleColor.Yellow,
        LogLevel.Information => ConsoleColor.Blue,
        LogLevel.Warning => ConsoleColor.DarkYellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.DarkRed,
        _ => _defaultColor,
    };

    private static bool IsCustomEvent(LogEvent logEvent)
    {
        return logEvent.Level is LogLevel.Information && logEvent.Message.StartsWith(LoggerExtensions.CustomEventTag);
    }

    private bool IsLatestCategory(string currentCategory)
    {
        return string.Equals(_lastCategory, currentCategory, StringComparison.Ordinal);
    }

    private void WriteCategory(string category)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"{category}:");
        _lastCategory = category;
    }
}
