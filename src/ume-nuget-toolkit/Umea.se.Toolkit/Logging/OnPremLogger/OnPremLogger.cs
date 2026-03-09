using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Logging.OnPremLogger.Models;

namespace Umea.se.Toolkit.Logging.OnPremLogger;

/// <summary>
/// ILogger implementation that connects to ApplicationInsightsOnPremLogger.
/// </summary>
public class OnPremLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ApplicationConfigOnPremBase _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private HttpClient? _httpClient;
    private HttpClient HttpClient => _httpClient ??= _httpClientFactory.CreateClient(nameof(OnPremLogger));
    private readonly Dictionary<Type, string> _onPremLoggerEndpointMap = new()
    {
        {
            typeof(InformationLog), "information"
        },
        {
            typeof(WarningLog), "warning"
        },
        {
            typeof(ExceptionLog), "exception"
        },
        {
            typeof(CustomEventLog), "event"
        },
    };

    public OnPremLogger(IHttpClientFactory httpClientFactory, ApplicationConfigOnPremBase config, string categoryName)
    {
        _categoryName = categoryName;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel <= LogLevel.Information && !_categoryName.StartsWith("Umea.se"))
        {
            return false;
        }

        return logLevel
            is LogLevel.Information
            or LogLevel.Warning
            or LogLevel.Error;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || _config.Environment is EnvironmentNames.Local.Development)
        {
            return;
        }

        BaseLog log = GetLog(logLevel, state, exception, formatter);
        _ = SendLogToOnPremLogger(log);
    }

    private BaseLog GetLog<TState>(LogLevel logLevel,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);

        return logLevel switch
        {
            LogLevel.Information when message.StartsWith(LoggerExtensions.CustomEventTag)
                => GetCustomEventLog(state),

            LogLevel.Information
                => new InformationLog
                {
                    Application = _config.ApiTitleWithEnvironment,
                    Source = _categoryName,
                    Message = message,
                },

            LogLevel.Warning
                => new WarningLog
                {
                    Application = _config.ApiTitleWithEnvironment,
                    Source = _categoryName,
                    Message = message,
                },

            LogLevel.Error
                => new ExceptionLog
                {
                    Application = _config.ApiTitleWithEnvironment,
                    Source = _categoryName,
                    Message = exception?.Message ?? message,
                    ExceptionType = exception?.GetType().Name,
                    ExceptionMessage = exception?.Message is not null
                        ? message
                        : null,
                    ExceptionStackTrace = exception?.StackTrace,
                },

            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null),
        };
    }

    private CustomEventLog GetCustomEventLog(object? state)
    {
        const string customEventPropertyName = "microsoft.custom_event.name";
        IEnumerable<KeyValuePair<string, object?>> stateAsKvp = state as IEnumerable<KeyValuePair<string, object?>> ?? [];

        Dictionary<string, object?> args = stateAsKvp
            .Where(kvp => kvp.Key != "{OriginalFormat}")
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Dictionary<string, string> properties = args
            .Where(kvp => kvp.Value is string && kvp.Key != customEventPropertyName)
            .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value!);

        Dictionary<string, double> measurements = args
            .Where(kvp => kvp.Value is double)
            .ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value!);

        args.TryGetValue(customEventPropertyName, out object? eventNameArg);
        string? eventName = eventNameArg as string;
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        return new CustomEventLog
        {
            Application = _config.ApiTitleWithEnvironment,
            Source = _categoryName,
            Properties = properties,
            Measurements = measurements,
            EventName = eventName,
        };
    }

    private Task SendLogToOnPremLogger(BaseLog log)
    {
        return Task.Run(async () =>
        {
            try
            {
                HttpResponseMessage response = await HttpClient.PostAsJsonAsync(_onPremLoggerEndpointMap[log.GetType()], log);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        });
    }
}
