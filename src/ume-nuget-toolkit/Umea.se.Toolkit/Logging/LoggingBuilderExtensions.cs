using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Logging.ConsoleLogger;
using Umea.se.Toolkit.Logging.OnPremLogger;

namespace Umea.se.Toolkit.Logging;

internal static class LoggingBuilderExtensions
{
    internal static ILoggingBuilder RemoveLoggers(this ILoggingBuilder builder)
    {
        return builder.ClearProviders();
    }

    internal static ILoggingBuilder AddCustomConsoleLogger(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());

        return builder;
    }

    internal static ILoggingBuilder AddApplicationInsightsLogger(this ILoggingBuilder builder, ApplicationConfigFunctionsBase config)
    {
        builder.Services
            .AddOpenTelemetry()
            .UseAzureMonitorExporter(options =>
            {
                options.ConnectionString = config.ApplicationInsightsConnectionString;
            });

        return builder;
    }

    internal static ILoggingBuilder AddApplicationInsightsLogger(this ILoggingBuilder builder, ApplicationConfigCloudBase config)
    {
        builder.Services
            .AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = config.ApplicationInsightsConnectionString;
            });

        return builder;
    }

    internal static ILoggingBuilder AddOnPremLogger(this ILoggingBuilder builder, ApplicationConfigOnPremBase config)
    {
        builder.Services
            .AddHttpClient(nameof(OnPremLogger.OnPremLogger), httpClient =>
            {
                httpClient.BaseAddress = new Uri(config.OnPremLoggerUrl);
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.OnPremLoggerKey);
            });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OnPremLoggerProvider>());

        return builder;
    }
}
