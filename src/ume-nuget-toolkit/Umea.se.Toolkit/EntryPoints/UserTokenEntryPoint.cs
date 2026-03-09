using Microsoft.Extensions.DependencyInjection;
using Umea.se.Toolkit.UserFromToken;

namespace Umea.se.Toolkit.EntryPoints;

public static class UserTokenEntryPoint
{
    /// <summary>
    /// Sets up injection for the logged-in <see cref="UserToken">UserToken</see>. It provides access to the information stored in the token in the current http request.
    /// </summary>
    public static IServiceCollection AddUserFromToken(this IServiceCollection services)
    {
        return services.AddTransient<UserToken>();
    }
}
