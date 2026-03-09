using System.Reflection;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umea.se.TestToolkit.Mocks;
using Umea.se.TestToolkit.TestInfrastructure.Exceptions;
using Umea.se.Toolkit.Auth;
using Umea.se.Toolkit.ClockInterface;

namespace Umea.se.TestToolkit.TestInfrastructure;

/// <summary>
/// This inherits from WebApplicationFactory and sets up our standard mocks: HttpClients, LoggedInUser and Clock.
/// It also provides the MockManagerBase for setting up mocks.
/// Use this directly or inherit from it if you want further customization or mocking.
/// </summary>
/// <typeparam name="TProgram">
/// The Program class
/// </typeparam>
/// <typeparam name="TClients">
/// Hand in your HttpClientNames class, where all client names are public const string {clientName}
/// Using reflection a mock will be created for all client names with url https://{clientName}.mock/
/// </typeparam>
public class WebAppFactoryBase<TProgram, TClients> : WebApplicationFactory<TProgram>
    where TProgram : class
    where TClients : class
{
    private readonly string _testProjectRootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseContentRoot(_testProjectRootDir);
        builder.UseEnvironment("unittests");
        builder.UseSetting("suppressKeyVaultConfigs", "true");

        EnsureUnittestAppsettingsExists();

        builder.ConfigureTestServices(services => services
            .AddSingleton<MockManagerCloud>()
            .AddSingleton<MockManagerOnPrem>()
            .AddSingleton(new HttpClientFactoryMock(DetermineClientNames<TClients>()))
            .AddSingleton<IHttpClientFactory>(provider => provider.GetRequiredService<HttpClientFactoryMock>())
            .AddSingleton<LoggedInUserMock>()
            .AddSingleton<IPolicyEvaluator, LoggedInUserMock>(provider => provider.GetRequiredService<LoggedInUserMock>())
            .AddSingleton<ClockMock>()
            .AddSingleton<IClock, ClockMock>(provider => provider.GetRequiredService<ClockMock>())
            .AddScoped<IApiKeyAuthorizer, ApiKeyAuthorizerMock>()
        );
    }

    private void EnsureUnittestAppsettingsExists()
    {
        string appsettingsPath = Path.Combine(_testProjectRootDir, "appsettings.unittests.json");

        if (File.Exists(appsettingsPath) == false)
        {
            throw new NoAppsettingsException("No appsettings.unittests.json file found in the root directory of the test project. Create such file with all appsettings necessary for the tests to run.");
        }
    }

    private static List<string> DetermineClientNames<T>()
    {
        return [.. typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string ?? "")];
    }
}
