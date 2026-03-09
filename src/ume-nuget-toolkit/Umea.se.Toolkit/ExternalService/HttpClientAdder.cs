using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Umea.se.Toolkit.EntryPoints;
using Umea.se.Toolkit.KeyVault;

namespace Umea.se.Toolkit.ExternalService;

internal static class HttpClientAdder
{
    internal static IServiceCollection Add(IServiceCollection services, string clientName, Action<HttpClientOptions>? configureOptions)
    {
        HttpClientOptions options = new();
        configureOptions?.Invoke(options);

        services
            .AddHttpClient(clientName)
            .ConfigureHttpClient(httpClient =>
            {
                if (options.BaseAddress is not null)
                {
                    httpClient.BaseAddress = new Uri(options.BaseAddress);
                }

                if (options.XApiKey is not null)
                {
                    httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
                    httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.XApiKey);
                }

                if (options.DefaultRequestHeaders.Count > 0)
                {
                    foreach (KeyValuePair<string, string> header in options.DefaultRequestHeaders)
                    {
                        if (string.IsNullOrWhiteSpace(header.Key))
                        {
                            continue;
                        }

                        httpClient.DefaultRequestHeaders.Remove(header.Key);
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                HttpClientHandler handler = new();

                if (options.CertificateName is not null)
                {
                    X509Certificate2 certificate = KeyVaultService.GetCertificate(options.CertificateName);

                    handler.SslProtocols = SslProtocols.Tls12;
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.ClientCertificates.Add(certificate);
                }

                return handler;
            });

        return services;
    }
}
