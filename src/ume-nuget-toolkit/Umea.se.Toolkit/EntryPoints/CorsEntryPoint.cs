using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Umea.se.Toolkit.EntryPoints;

public static class CorsEntryPoint
{
    private const string AllowedOriginsCorsPolicy = "AllowedOriginsCorsPolicy";

    /// <summary>
    /// Adds a CORS policy that allows requests from specified origins.
    /// <br/>
    /// Call <see cref="UseAllowedOriginsCorsPolicy"/> to use the policy.
    /// </summary>
    public static IServiceCollection AddAllowedOriginsCorsPolicy(this IServiceCollection services, string[] allowedOrigins)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(AllowedOriginsCorsPolicy, builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Use the CORS policy that is created by <see cref="AddAllowedOriginsCorsPolicy"/>.
    /// </summary>
    public static IApplicationBuilder UseAllowedOriginsCorsPolicy(this IApplicationBuilder app)
    {
        return app.UseCors(AllowedOriginsCorsPolicy);
    }
}
