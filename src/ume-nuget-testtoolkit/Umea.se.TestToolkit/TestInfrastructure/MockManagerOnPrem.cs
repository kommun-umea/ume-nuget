using Umea.se.TestToolkit.Mocks;
using Umea.se.TestToolkit.TestInfrastructure.BaseClasses;

namespace Umea.se.TestToolkit.TestInfrastructure;

/// <summary>
/// Use this class in your tests to set up the mocks provided by this package.
/// </summary>
public class MockManagerOnPrem(ClockMock clockMock, HttpClientFactoryMock clientFactoryMock)
    : MockManagerBase(clockMock, clientFactoryMock)
{
    public override MockManagerOnPrem SetClockTime(DateTime time)
    {
        base.SetClockTime(time);
        return this;
    }

    public override MockManagerOnPrem SetupHttpClient(string httpClientName, Action<HttpMessageHandlerMock> clientSetup)
    {
        base.SetupHttpClient(httpClientName, clientSetup);
        return this;
    }
}
