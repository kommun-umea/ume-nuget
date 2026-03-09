using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;

namespace Umea.se.Toolkit.Functions;

public static class FunctionsApplicationBuilderExtensions
{
    extension(FunctionsApplicationBuilder builder)
    {
        /// <summary>
        /// Registers appsettings(.env).json and configures the worker to use the ASP.NET Core integration.
        /// </summary>
        public FunctionsApplicationBuilder AddDefaultConfiguration()
        {
            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            builder.ConfigureFunctionsWebApplication();

            return builder;
        }
    }
}
