using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Umea.se.Toolkit.Configuration;

/// <summary>
/// Base ApplicationConfig for Azure Functions. Simplifies configuration retrieval from appsettings.json + local.settings.json.
/// This is an extension of <see cref="ApplicationConfigCloudBase"/>.
/// </summary>
public abstract class ApplicationConfigFunctionsBase(IConfiguration configuration, Assembly? entryAssembly = null)
    : ApplicationConfigCloudBase(configuration, entryAssembly)
{
    public override string Environment => GetValue("AZURE_FUNCTIONS_ENVIRONMENT");
    public new string ApplicationInsightsConnectionString => GetValue("APPLICATIONINSIGHTS_CONNECTION_STRING");
}
