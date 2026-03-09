using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Umea.se.Toolkit.Auth;

/// <summary>
/// Authorization attribute that can be applied to endpoints and controllers.
/// When applied, require "X-Api-Key" in header to be equal to value of one of the configured API keys.
/// <br/>
/// API keys can be configured in appsettings.json under Api:Keys.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class AuthorizeApiKeyAttribute(string name = "Default") : Attribute, IAsyncAuthorizationFilter
{
    public readonly string Name = name;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        IApiKeyAuthorizer authorizer = GetAuthorizer(context);

        return authorizer.AuthorizeAsync(context);
    }

    private static IApiKeyAuthorizer GetAuthorizer(AuthorizationFilterContext context)
    {
        return context.HttpContext.RequestServices
            .GetRequiredService<IApiKeyAuthorizer>();
    }
}
