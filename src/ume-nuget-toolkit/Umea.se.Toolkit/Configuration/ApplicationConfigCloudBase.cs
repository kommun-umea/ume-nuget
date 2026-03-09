using System.Reflection;
using Microsoft.Extensions.Configuration;
using Umea.se.Toolkit.KeyVault;

namespace Umea.se.Toolkit.Configuration;

/// <summary>
/// Base ApplicationConfig for cloud APIs. Simplifies configuration retrieval from appsettings.json.
/// This is an extension of <see cref="ApplicationConfigBase"/>.
/// </summary>
public abstract class ApplicationConfigCloudBase(IConfiguration configuration, Assembly? entryAssembly = null)
    : ApplicationConfigBase(
        ShouldLoadKeyVaultSecrets(configuration)
            ? LoadKeyVaultSecrets(configuration)
            : configuration,
        entryAssembly)
{
    private static bool ShouldLoadKeyVaultSecrets(IConfiguration configuration)
    {
        bool keyVaultUrlExists = !string.IsNullOrEmpty(configuration["KeyVaultUrl"]);
        bool shouldSuppressKeyVaultConfigs = configuration.GetValue<bool?>("SuppressKeyVaultConfigs") == true;

        return keyVaultUrlExists && shouldSuppressKeyVaultConfigs == false;
    }

    private static IConfiguration LoadKeyVaultSecrets(IConfiguration configuration)
    {
        KeyVaultService.ConnectToKeyVault(configuration["KeyVaultUrl"]!);
        ApplicationConfigSecretsHandler.LoadKeyVaultSecrets(configuration);

        return configuration;
    }

    public override bool IsEnvironmentSafe => Environment is EnvironmentNames.Local.Development or EnvironmentNames.Cloud.Dev;

    public string KeyVaultUrl => GetValue("KeyVaultUrl");
    public bool SuppressKeyVaultConfigs => TryGetBool("suppressKeyVaultConfigs") ?? false;
    public string ApplicationInsightsConnectionString => GetValue("ApplicationInsights:ConnectionString");
}
