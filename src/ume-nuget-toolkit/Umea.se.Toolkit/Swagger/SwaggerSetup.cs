using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;
using Umea.se.Toolkit.Auth;
using Umea.se.Toolkit.CommonModels.Exceptions;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.Swagger;

internal static class SwaggerSetup
{
    internal static IServiceCollection AddSwagger(IServiceCollection services, ApplicationConfigBase config)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.EnableAnnotations();
            options.OrderActionsBy(api => $"{api.ActionDescriptor.RouteValues["controller"]}_{api.HttpMethod}_{api.RelativePath}");

            options.SwaggerDoc(config.ApiVersion, new OpenApiInfo
            {
                Title = config.ApiTitleWithEnvironment,
                Version = config.ApiVersion,
                Description = config.ApiDescription,
            });

            string xmlFileName = $"{Assembly.GetEntryAssembly()?.GetName().Name ?? throw new InvalidEntryAssemblyException()}.xml";
            string xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
            if (File.Exists(xmlFilePath))
            {
                options.IncludeXmlComments(xmlFilePath);
            }

            if (DoesApiContainAttribute<AuthorizeAttribute>())
            {
                options.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token_value}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                    });
            }

            if (DoesApiContainAttribute<AuthorizeApiKeyAttribute>())
            {
                options.AddSecurityDefinition("ApiKey",
                    new OpenApiSecurityScheme
                    {
                        Description = "API Key Authentication",
                        Name = "X-Api-Key",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "ApiKey",
                    });
            }

            options.OperationFilter<AuthorizationOperationFilter>();
        });

        return services;
    }

    internal static IApplicationBuilder UseSwagger(IApplicationBuilder app, ApplicationConfigBase config, string? azureConnectionPrefix)
    {
        app.UseSwagger(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
        });
        app.UseSwaggerUI(options =>
        {
            string defaultSwaggerEndpoint = $"/swagger/{config.ApiVersion}/swagger.json";

            options.DocumentTitle = config.ApiNameWithEnvironment;
            options.RoutePrefix = string.Empty;

            // Add AzureConnection Swagger UI if OnPrem
            if (azureConnectionPrefix is not null && config.Environment is not EnvironmentNames.Local.Development)
            {
                options.SwaggerEndpoint($"/{azureConnectionPrefix}{defaultSwaggerEndpoint}", $"{config.ApiTitle} AzureConnection");
            }

            options.SwaggerEndpoint(defaultSwaggerEndpoint, config.ApiTitle);

            options.DisplayRequestDuration();
            options.EnableTryItOutByDefault();
            options.EnableDeepLinking();
            options.EnablePersistAuthorization();

            // Collapse controllers if there are more than 10 endpoints
            options.DocExpansion(ApiEndpointsCount() switch
            {
                > 10 => DocExpansion.None,
                _ => DocExpansion.List,
            });

            // Invert Swagger UI's icons (locked/unlocked, on/off)
            options.HeadContent = """
                <style>
                    .swagger-ui .authorization__btn .unlocked {
                        opacity: 0.75;
                    }
                    
                    .swagger-ui .authorization__btn .unlocked path, .btn.authorize.unlocked svg path {
                        d: path('M 15.8 8 H 14 V 5.6 C 14 2.703 12.665 1 10 1 C 7.334 1 6 2.703 6 5.6 V 8 H 4 c -0.553 0 -1 0.646 -1 1.199 V 17 c 0 0.549 0.428 1.139 0.951 1.307 l 1.197 0.387 C 5.672 18.861 6.55 19 7.1 19 h 5.8 c 0.549 0 1.428 -0.139 1.951 -0.307 l 1.196 -0.387 c 0.524 -0.167 0.953 -0.757 0.953 -1.306 V 9.199 C 17 8.646 16.352 8 15.8 8 Z M 12 8 H 8 V 5.199 C 8 3.754 8.797 3 10 3 c 1.203 0 2 0.754 2 2.199 V 8 Z');
                    }
             
                    .swagger-ui .authorization__btn .locked {
                        opacity: 0.5;
                    }
                    
                    .swagger-ui .authorization__btn .locked path, .btn.authorize.locked svg path {
                        d: path('M 15.8 8 H 14 V 5.6 C 14 2.703 12.665 1 10 1 C 7.334 1 6 2.703 6 5.6 V 6 h 2 v -0.801 C 8 3.754 8.797 3 10 3 c 1.203 0 2 0.754 2 2.199 V 8 H 4 c -0.553 0 -1 0.646 -1 1.199 V 17 c 0 0.549 0.428 1.139 0.951 1.307 l 1.197 0.387 C 5.672 18.861 6.55 19 7.1 19 h 5.8 c 0.549 0 1.428 -0.139 1.951 -0.307 l 1.196 -0.387 c 0.524 -0.167 0.953 -0.757 0.953 -1.306 V 9.199 C 17 8.646 16.352 8 15.8 8 Z');
                    }
                    
                    .btn.authorize.locked svg path {
                        opacity: 0.75;
                    }
                    
                    html .dark-mode-toggle svg path {
                        d: path('M12 2a7 7 0 0 0-7 7c0 2.38 1.19 4.47 3 5.74V17a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1v-2.26c1.81-1.27 3-3.36 3-5.74a7 7 0 0 0-7-7M9 21a1 1 0 0 0 1 1h4a1 1 0 0 0 1-1v-1H9z');
                    }
                    
                    html.dark-mode .dark-mode-toggle svg path {
                        d: path('M12 2C9.76 2 7.78 3.05 6.5 4.68l9.81 9.82C17.94 13.21 19 11.24 19 9a7 7 0 0 0-7-7M3.28 4 2 5.27 5.04 8.3C5 8.53 5 8.76 5 9c0 2.38 1.19 4.47 3 5.74V17a1 1 0 0 0 1 1h5.73l4 4L20 20.72zM9 20v1a1 1 0 0 0 1 1h4a1 1 0 0 0 1-1v-1z');
                    }
                </style>
            """;
        });

        return app;
    }

    private static bool DoesApiContainAttribute<T>() where T : Attribute
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? throw new InvalidEntryAssemblyException();
        List<Type> controllers = [.. assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))];

        if (controllers.Any(HasAttribute<T>))
        {
            return true;
        }

        IEnumerable<MethodInfo> methods = controllers.SelectMany(controller => controller.GetMethods());

        return methods.Any(HasAttribute<T>);
    }

    private static bool HasAttribute<T>(this ICustomAttributeProvider type) where T : Attribute
    {
        return type.GetCustomAttributes(true).OfType<T>().Any();
    }

    private static int ApiEndpointsCount()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? throw new InvalidEntryAssemblyException();
        IEnumerable<Type> controllers = assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type));

        // Count all methods with an attribute that starts with "Http" in all controllers
        return controllers.Sum(controller =>
            controller.GetMethods().Count(method =>
                method.GetCustomAttributes().Any(attribute =>
                    attribute.GetType().Name.StartsWith("Http"))));
    }
}
