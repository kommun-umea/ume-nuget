using Microsoft.AspNetCore.Mvc.Filters;

namespace Umea.se.Toolkit.Auth;

public interface IApiKeyAuthorizer
{
    public Task AuthorizeAsync(AuthorizationFilterContext context);
}
