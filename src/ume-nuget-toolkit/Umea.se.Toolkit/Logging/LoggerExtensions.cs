using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Logging.Models;

namespace Umea.se.Toolkit.Logging;

public static class LoggerExtensions
{
    public const string CustomEventTag = "<CustomEvent>";

    /// <summary>
    /// Register customEvent log in Application Insights.
    /// </summary>
    public static void LogCustomEvent(this ILogger logger, string eventName, Action<CustomEventOptions>? options = null)
    {
        CustomEventOptions customEventOptions = new();
        options?.Invoke(customEventOptions);

        string messageTemplate = CustomEventTag
            + "EventName={microsoft.custom_event.name}"
            + customEventOptions.GetMessageTemplateString();

        object[] args =
        [
            eventName,
            ..customEventOptions.GetValues(),
        ];

        // Logs are converted to customEvents if the message contains "{microsoft.custom_event.name}"
        // https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter#customevents
        logger.LogInformation(messageTemplate, args);
    }
}

