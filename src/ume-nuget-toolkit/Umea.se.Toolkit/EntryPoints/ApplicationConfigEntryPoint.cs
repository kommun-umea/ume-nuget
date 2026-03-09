using Microsoft.Extensions.DependencyInjection;
using Umea.se.Toolkit.Auth;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.EntryPoints;

public static class ApplicationConfigEntryPoint
{
    /// <summary>
    /// Add ApplicationConfig to dependency injection (derived from <see cref="ApplicationConfigCloudBase"/> or <see cref="ApplicationConfigOnPremBase"/>).
    /// </summary>
    public static IServiceCollection AddApplicationConfig<T>(this IServiceCollection services, T config)
        where T : ApplicationConfigBase
    {
        services
            .AddSingleton(config)
            .AddSingleton<ApplicationConfigBase>(provider => provider.GetRequiredService<T>())
            .AddScoped<IApiKeyAuthorizer, ApiKeyAuthorizer>();

        switch (config)
        {
            case ApplicationConfigCloudBase:
                services.AddSingleton<ApplicationConfigCloudBase>(provider =>
                {
                    ApplicationConfigCloudBase? cloudConfig = provider.GetRequiredService<T>() as ApplicationConfigCloudBase;

                    return cloudConfig ?? throw new InvalidOperationException();
                });
                break;

            case ApplicationConfigOnPremBase:
                services.AddSingleton<ApplicationConfigOnPremBase>(provider =>
                {
                    ApplicationConfigOnPremBase? onPremConfig = provider.GetRequiredService<T>() as ApplicationConfigOnPremBase;

                    return onPremConfig ?? throw new InvalidOperationException();
                });
                break;
        }

        return services;
    }
}
