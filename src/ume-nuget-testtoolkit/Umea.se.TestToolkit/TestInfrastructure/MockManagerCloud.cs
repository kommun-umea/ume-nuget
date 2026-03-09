using Umea.se.TestToolkit.Mocks;
using Umea.se.TestToolkit.TestInfrastructure.BaseClasses;

namespace Umea.se.TestToolkit.TestInfrastructure;

/// <summary>
/// Use this class in your tests to set up the mocks provided by this package.
/// </summary>
public class MockManagerCloud(LoggedInUserMock loggerInUserMock, ClockMock clockMock, HttpClientFactoryMock clientFactoryMock)
    : MockManagerBase(clockMock, clientFactoryMock)
{
    public MockManagerCloud SetupUser(Action<LoggedInUserMock> authSetup)
    {
        authSetup(loggerInUserMock);
        return this;
    }

    public override MockManagerCloud SetClockTime(DateTime time)
    {
        base.SetClockTime(time);
        return this;
    }

    public override MockManagerCloud SetupHttpClient(string httpClientName, Action<HttpMessageHandlerMock> clientSetup)
    {
        base.SetupHttpClient(httpClientName, clientSetup);
        return this;
    }
}
