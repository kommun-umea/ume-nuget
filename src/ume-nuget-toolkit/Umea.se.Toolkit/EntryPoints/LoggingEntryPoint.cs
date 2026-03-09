using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Logging;

namespace Umea.se.Toolkit.EntryPoints;

public static class LoggingEntryPoint
{
    /// <summary>
    /// Use default loggers for Azure Function applications.
    /// <br/>- Azure Application Insights logger
    /// </summary>
    public static ILoggingBuilder UseDefaultLoggers(this ILoggingBuilder builder, ApplicationConfigFunctionsBase config)
    {
        return builder
            .RemoveLoggers()
            .AddApplicationInsightsLogger(config);
    }

    /// <summary>
    /// Use default loggers for cloud applications.
    /// <br/>- Custom console logger
    /// <br/>- Azure Application Insights logger
    /// </summary>
    public static ILoggingBuilder UseDefaultLoggers(this ILoggingBuilder builder, ApplicationConfigCloudBase config)
    {
        return builder
            .RemoveLoggers()
            .AddCustomConsoleLogger()
            .AddApplicationInsightsLogger(config);
    }

    /// <summary>
    /// Use default loggers for onprem applications.
    /// <br/>- Custom console logger
    /// <br/>- Azure Application Insights logger via on-prem setup
    /// </summary>
    public static ILoggingBuilder UseDefaultLoggers(this ILoggingBuilder builder, ApplicationConfigOnPremBase config)
    {
        return builder
            .RemoveLoggers()
            .AddCustomConsoleLogger()
            .AddOnPremLogger(config);
    }

    /// <summary>
    /// Add custom console logger.
    /// </summary>
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder)
    {
        return builder.AddCustomConsoleLogger();
    }
}
