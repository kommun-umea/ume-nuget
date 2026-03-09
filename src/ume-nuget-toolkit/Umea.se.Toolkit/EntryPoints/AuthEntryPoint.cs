using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Umea.se.Toolkit.EntryPoints;

public static class AuthEntryPoint
{
    /// <summary>
    /// Adds all authPolicies in the list of pairs.
    /// </summary>
    /// <param name="services">Builder.Services</param>
    /// <param name="authPolicies">List of policy names and guids from the config</param>
    public static IServiceCollection AddAuthPolicies(this IServiceCollection services, (string, string)[] authPolicies)
    {
        AuthorizationBuilder builder = services.AddAuthorizationBuilder();
        foreach ((string name, string cfg) in authPolicies)
        {
            builder.AddPolicy(name, policy => policy.RequireAssertion(context => context.User.HasClaim(claim => claim.Type == "groups" && claim.Value.Contains(cfg))));
        }

        return services;
    }
}
