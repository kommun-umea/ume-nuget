using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Umea.se.Toolkit.Configuration;

/// <summary>
/// Base ApplicationConfig for on-prem APIs. Simplifies configuration retrieval from appsettings.json.
/// This is an extension of <see cref="ApplicationConfigBase"/>.
/// </summary>
public abstract class ApplicationConfigOnPremBase(IConfiguration configuration, Assembly? entryAssembly = null)
    : ApplicationConfigBase(configuration, entryAssembly)
{
    public override bool IsEnvironmentSafe => Environment is EnvironmentNames.Local.Development or EnvironmentNames.OnPrem.Test;

    public string AzureConnectionPrefix => GetValue("Api:AzureConnectionPrefix");
    public string OnPremLoggerUrl => GetValue("OnPremLogger:Url");
    public string OnPremLoggerKey => GetValue("OnPremLogger:Key");
}
