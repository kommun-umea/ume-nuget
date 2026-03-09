using Microsoft.Extensions.DependencyInjection;
using Umea.se.Toolkit.ExternalService;

namespace Umea.se.Toolkit.EntryPoints;

public static class HttpClientEntryPoint
{
    /// <summary>
    /// Sets up injection of an HttpClient with a name and configurable <see cref="HttpClientOptions"/>.
    /// These can be injected via IHttpClientFactory, as e.g. done in <see cref="ExternalServiceBase"/>
    /// </summary>
    public static IServiceCollection AddDefaultHttpClient(this IServiceCollection services, string clientName, Action<HttpClientOptions>? configureOptions = null)
    {
        return HttpClientAdder.Add(services, clientName, configureOptions);
    }
}
