using Microsoft.AspNetCore.Mvc.Filters;
using Umea.se.Toolkit.Auth;

namespace Umea.se.TestToolkit.Mocks;

public class ApiKeyAuthorizerMock : IApiKeyAuthorizer
{
    public Task AuthorizeAsync(AuthorizationFilterContext context)
    {
        return Task.CompletedTask;
    }
}
