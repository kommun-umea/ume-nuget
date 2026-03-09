using Umea.se.TestToolkit.Mocks;

namespace Umea.se.TestToolkit.TestInfrastructure.BaseClasses;

public abstract class MockManagerBase(ClockMock clockMock, HttpClientFactoryMock clientFactoryMock)
{
    public virtual MockManagerBase SetClockTime(DateTime time)
    {
        clockMock.MockedNow = time;
        return this;
    }

    public virtual MockManagerBase SetupHttpClient(string httpClientName, Action<HttpMessageHandlerMock> clientSetup)
    {
        HttpMessageHandlerMock clientMock = clientFactoryMock.GetMessageHandlerMock(httpClientName);
        clientSetup(clientMock);
        return this;
    }

    public List<(HttpMethod method, Uri url, string? body)> HttpCallsToMock(string httpClientName)
    {
        HttpMessageHandlerMock clientMock = clientFactoryMock.GetMessageHandlerMock(httpClientName);
        return clientMock.Calls;
    }
}
