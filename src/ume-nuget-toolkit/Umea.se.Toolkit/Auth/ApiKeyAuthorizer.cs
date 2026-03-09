using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Filters;
using Umea.se.Toolkit.Logging;

namespace Umea.se.Toolkit.Auth;

public class ApiKeyAuthorizer : IApiKeyAuthorizer
{
    private const string Header = "X-Api-Key";
    private const string IsProcessedFlag = "Umea.AuthorizeApiKey.IsProcessed";

    public Task AuthorizeAsync(AuthorizationFilterContext context)
    {
        IDictionary<object, object?> sharedRequestData = context.HttpContext.Items;

        // Only handle authorization once per request
        if (sharedRequestData.ContainsKey(IsProcessedFlag))
        {
            return Task.CompletedTask;
        }

        sharedRequestData[IsProcessedFlag] = true;
        ILogger logger = GetLogger(context);
        ApplicationConfigBase config = GetApplicationConfig(context);

        if (IsAnonymousAllowed(context))
        {
            return Task.CompletedTask;
        }

        string? requestedApiKeyValue = GetRequestedApiKeyValue(context);
        if (string.IsNullOrWhiteSpace(requestedApiKeyValue))
        {
            context.Result = GetObjectResult(HttpResponseFactoryBase.Unauthorized_ApiKeyMissing());
            LogApiKeyAuthorization(logger, context, "Missing");
            return Task.CompletedTask;
        }

        List<string> allowedKeys = GetAllowedKeysOnEndpoint(context);
        string? requestedApiKeyName = IdentifyApiKeyName(config, requestedApiKeyValue);

        if (requestedApiKeyName is null || !allowedKeys.Contains(requestedApiKeyName))
        {
            context.Result = GetObjectResult(HttpResponseFactoryBase.Forbidden_ApiKeyInvalid());
            LogApiKeyAuthorization(logger, context, "Invalid", requestedApiKeyName);
            return Task.CompletedTask;
        }

        LogApiKeyAuthorization(logger, context, "Success", requestedApiKeyName);
        return Task.CompletedTask;
    }

    private static ILogger GetLogger(AuthorizationFilterContext context)
    {
        return context.HttpContext.RequestServices.GetRequiredService<ILogger<AuthorizeApiKeyAttribute>>();
    }

    private static ApplicationConfigBase GetApplicationConfig(AuthorizationFilterContext context)
    {
        return context.HttpContext.RequestServices.GetRequiredService<ApplicationConfigBase>();
    }

    private static ObjectResult GetObjectResult(HttpResponseException exception)
    {
        return new ObjectResult(exception.ResponseBody)
        {
            StatusCode = (int)exception.StatusCode,
        };
    }

    private static bool IsAnonymousAllowed(AuthorizationFilterContext context)
    {
        Endpoint? endpoint = context.HttpContext.GetEndpoint();
        return endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
    }

    private static List<string> GetAllowedKeysOnEndpoint(AuthorizationFilterContext context)
    {
        Endpoint? endpoint = context.HttpContext.GetEndpoint();

        List<string>? allowedKeys = endpoint?.Metadata
                .GetOrderedMetadata<AuthorizeApiKeyAttribute>()
                .Select(a => a.Name)
                .Distinct()
                .ToList();

        return allowedKeys ?? [];
    }

    private static string? GetRequestedApiKeyValue(AuthorizationFilterContext context)
    {
        StringValues value = context.HttpContext.Request.Headers[Header];

        return value.Equals(StringValues.Empty) ? null : value.ToString();
    }

    private static string? IdentifyApiKeyName(ApplicationConfigBase config, string apiKeyValue)
    {
        return config.ApiKeys.FirstOrDefault(k => k.Value.Equals(apiKeyValue)).Key;
    }

    private static void LogApiKeyAuthorization(ILogger logger, AuthorizationFilterContext context, string result, string? apiKeyName = null)
    {
        logger.LogCustomEvent("ApiKeyAuthorization", options =>
        {
            options
                .WithProperty("Result", result)
                .WithProperty("Method", context.HttpContext.Request.Method)
                .WithProperty("Path", context.HttpContext.Request.Path);

            if (context.ActionDescriptor.RouteValues.TryGetValue("controller", out string? controller))
            {
                options.WithProperty("Controller", controller ?? string.Empty);
            }

            if (context.ActionDescriptor.RouteValues.TryGetValue("action", out string? action))
            {
                options.WithProperty("Action", action ?? string.Empty);
            }

            if (apiKeyName is not null)
            {
                options.WithProperty("KeyName", apiKeyName);
            }
        });
    }
}
