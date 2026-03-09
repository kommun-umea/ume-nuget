using Umea.se.TestToolkit.Exceptions;

namespace Umea.se.TestToolkit.Mocks;

public class HttpClientFactoryMock : IHttpClientFactory
{
    private readonly Dictionary<string, string> _clientUrls;
    private readonly Dictionary<string, HttpMessageHandlerMock> _clientMocks;

    internal HttpClientFactoryMock(List<string> clientNames)
    {
        _clientUrls = clientNames.ToDictionary(n => n, n => $"https://{n}.mock/");
        _clientMocks = clientNames.ToDictionary(n => n, n => new HttpMessageHandlerMock(_clientUrls[n]));
    }

    public HttpClient CreateClient(string clientName)
    {
        try
        {
            return new HttpClient(_clientMocks[clientName]) { BaseAddress = new Uri(_clientUrls[clientName]) };
        }
        catch (Exception ex)
        {
            throw new MockNotSetupException($"Could not find mocked httpClient for {clientName}. Check TClients passed to WebAppFactoryBase and ControllerTestBase.", ex);
        }
    }

    internal HttpMessageHandlerMock GetMessageHandlerMock(string clientName)
    {
        try
        {
            return _clientMocks[clientName];
        }
        catch (Exception ex)
        {
            throw new MockNotSetupException($"Could not find mocked httpClient for {clientName}. Check TClients passed to WebAppFactoryBase and ControllerTestBase.", ex);
        }
    }
}
