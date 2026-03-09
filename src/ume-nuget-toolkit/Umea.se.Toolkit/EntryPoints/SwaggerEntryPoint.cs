using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Swagger;

namespace Umea.se.Toolkit.EntryPoints;

public static class SwaggerEntryPoint
{
    /// <summary>
    /// Add Swagger with authorization support to the service collection.
    /// </summary>
    public static IServiceCollection AddDefaultSwagger(this IServiceCollection services, ApplicationConfigBase config)
    {
        return SwaggerSetup.AddSwagger(services, config);
    }

    /// <summary>
    /// Add Swagger UI to generic application.
    /// </summary>
    public static IApplicationBuilder UseDefaultSwagger(this IApplicationBuilder app, ApplicationConfigBase config)
    {
        return SwaggerSetup.UseSwagger(app, config, null);
    }

    /// <summary>
    /// Add Swagger UI to cloud application.
    /// </summary>
    public static IApplicationBuilder UseDefaultSwagger(this IApplicationBuilder app, ApplicationConfigCloudBase config)
    {
        return SwaggerSetup.UseSwagger(app, config, null);
    }

    /// <summary>
    /// Add Swagger UI to on-prem application.
    /// </summary>
    public static IApplicationBuilder UseDefaultSwagger(this IApplicationBuilder app, ApplicationConfigOnPremBase config)
    {
        return SwaggerSetup.UseSwagger(app, config, config.AzureConnectionPrefix);
    }
}
