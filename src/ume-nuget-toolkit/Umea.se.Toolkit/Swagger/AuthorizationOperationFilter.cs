using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umea.se.Toolkit.Auth;

namespace Umea.se.Toolkit.Swagger;

/// <summary>
/// Show proper authorization method for each endpoint in Swagger UI.
/// </summary>
internal class AuthorizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // In case of attribute [AllowAnonymous]
        if (CurrentOperationHasAttribute<AllowAnonymousAttribute>(context))
        {
            return;
        }

        // In case of attribute [Authorize]
        if (CurrentOperationHasAttribute<AuthorizeAttribute>(context))
        {
            OpenApiSecurityRequirement requirement = new()
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [],
            };

            operation.Security ??= [];
            operation.Security.Add(requirement);
        }

        // In case of attribute [AuthorizeApiKey]
        if (CurrentOperationHasAttribute<AuthorizeApiKeyAttribute>(context))
        {
            OpenApiSecurityRequirement requirement = new()
            {
                [new OpenApiSecuritySchemeReference("ApiKey", context.Document)] = [],
            };

            operation.Security ??= [];
            operation.Security.Add(requirement);
        }
    }

    private static bool CurrentOperationHasAttribute<T>(OperationFilterContext context) where T : Attribute
    {
        return context.MethodInfo.GetCustomAttributes(true).OfType<T>().Any()
            || context.MethodInfo.DeclaringType!.GetCustomAttributes(true).OfType<T>().Any();
    }
}
