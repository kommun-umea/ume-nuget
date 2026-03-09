using Microsoft.Extensions.DependencyInjection;
using Umea.se.TestToolkit.TestInfrastructure.BaseClasses;

namespace Umea.se.TestToolkit.TestInfrastructure;

/// <summary>
/// Baseclass for OnPrem endpoint tests.
/// </summary>
/// <inheritdoc cref="ControllerTestBase&lt;TWebAppFactory, TProgram, TClients&gt;"/>
public class ControllerTestOnPrem<TWebAppFactory, TProgram, TClients>
    : ControllerTestBase<TWebAppFactory, TProgram, TClients>
    where TWebAppFactory : WebAppFactoryBase<TProgram, TClients>, new()
    where TProgram : class
    where TClients : class
{
    protected readonly MockManagerOnPrem MockManager;

    protected ControllerTestOnPrem()
    {
        MockManager = WebAppFactory.Services.GetRequiredService<MockManagerOnPrem>();
    }
}
