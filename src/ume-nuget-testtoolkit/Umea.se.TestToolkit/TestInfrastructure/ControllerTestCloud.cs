using Microsoft.Extensions.DependencyInjection;
using Umea.se.TestToolkit.TestInfrastructure.BaseClasses;

namespace Umea.se.TestToolkit.TestInfrastructure;

/// <summary>
/// Baseclass for Cloud endpoint tests.
/// </summary>
/// <inheritdoc cref="ControllerTestBase&lt;TWebAppFactory, TProgram, TClients&gt;"/>
public class ControllerTestCloud<TWebAppFactory, TProgram, TClients>
    : ControllerTestBase<TWebAppFactory, TProgram, TClients>
    where TWebAppFactory : WebAppFactoryBase<TProgram, TClients>, new()
    where TProgram : class
    where TClients : class
{
    protected readonly MockManagerCloud MockManager;

    protected ControllerTestCloud()
    {
        MockManager = WebAppFactory.Services.GetRequiredService<MockManagerCloud>();
    }
}
