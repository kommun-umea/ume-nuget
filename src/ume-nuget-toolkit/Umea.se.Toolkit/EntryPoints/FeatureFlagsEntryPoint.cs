using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Umea.se.Toolkit.Controllers;
using Umea.se.Toolkit.FeatureFlags;

namespace Umea.se.Toolkit.EntryPoints;

public static class FeatureFlagsEntryPoint
{
    /// <summary>
    /// Adds feature flag support using a simple list of feature names.
    /// Features can be specified as a comma-separated string (KeyVault / DevOps variable)
    /// or as a JSON array in appsettings.json.
    /// <para>
    /// This is compatible with the standard <c>feature_management</c> schema.
    /// Flags defined in <c>feature_management.feature_flags</c> take priority over the simple list,
    /// allowing you to graduate a flag to use filters, variants, or targeting.
    /// </para>
    /// <example>
    /// appsettings.json: <c>"Features": ["ErrorReport", "EstateService"]</c>
    /// <br/>
    /// KeyVault/env var: <c>Features=ErrorReport,EstateService</c>
    /// </example>
    /// </summary>
    /// <param name="services">builder.Services</param>
    /// <param name="sectionName">Configuration section name. Defaults to "Features".</param>
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, string sectionName = "Features")
    {
        services.AddSingleton<IFeatureDefinitionProvider>(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();

            SimpleFeatureDefinitionProvider simpleProvider = new(configuration, sectionName);
            ConfigurationFeatureDefinitionProvider standardProvider = new(configuration);

            // Simple flags first, standard schema overrides when both define the same flag
            return new CompositeFeatureDefinitionProvider([simpleProvider, standardProvider]);
        });

        services.AddFeatureManagement();

        services
            .AddControllers()
            .ConfigureApplicationPartManager(partManager =>
            {
                partManager.FeatureProviders.Add(new ExplicitControllersFeatureProvider(typeof(FeaturesController)));
            });

        return services;
    }
}
