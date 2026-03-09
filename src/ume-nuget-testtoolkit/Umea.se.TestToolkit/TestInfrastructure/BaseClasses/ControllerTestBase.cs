namespace Umea.se.TestToolkit.TestInfrastructure.BaseClasses;

/// <summary>
/// Baseclass for endpoint tests. See WebAppFactoryBase.
/// </summary>
/// <typeparam name="TWebAppFactory">
/// The WebApplicationFactory used for the tests. It can be the one provided in TestToolkit or it can inherit from it.
/// If you inherit, make sure that the other two params here fit to the ones used in the WebApplicationFactory.
/// </typeparam>
/// <typeparam name="TProgram">
/// The Program class
/// </typeparam>
/// <typeparam name="TClients">
/// Hand in your HttpClientNames class, where all client names are public const string {clientName}
/// Using reflection a mock will be created for all client names with url https://{clientName}.mock/
/// </typeparam>
public abstract class ControllerTestBase<TWebAppFactory, TProgram, TClients>
    where TWebAppFactory : WebAppFactoryBase<TProgram, TClients>, new()
    where TProgram : class
    where TClients : class
{
    protected readonly HttpClient Client;
    protected readonly TWebAppFactory WebAppFactory;

    protected ControllerTestBase()
    {
        WebAppFactory = new TWebAppFactory();
        Client = WebAppFactory.CreateClient();
    }
}
