using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Umea.se.Toolkit.Auth;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Test.Infrastructure;

namespace Umea.se.Toolkit.Test;

public class AuthorizeApiKeyAttributeTests
{
    private const string Header = "X-Api-Key";

    [Fact]
    public async Task Allows_When_Anonymous_Metadata_Is_Present()
    {
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithEndpoint("E1", e1 => e1
                    .WithAllowAnonymous()))
            .BuildAssembly();
        TestApplicationConfig config = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .BuildConfig(assembly);

        AuthorizationFilterContext context = GetContext(assembly, config, "C1", "E1");
        AuthorizeApiKeyAttribute attribute = GetApiKeyAttribute(context.HttpContext);

        await attribute.OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task Returns_401_When_Header_Missing()
    {
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithEndpoint("E1"))
            .BuildAssembly();
        TestApplicationConfig config = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .BuildConfig(assembly);

        AuthorizationFilterContext context = GetContext(assembly, config, "C1", "E1");
        AuthorizeApiKeyAttribute attribute = GetApiKeyAttribute(context.HttpContext);

        await attribute.OnAuthorizationAsync(context);

        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Returns_403_When_Header_Present_But_Not_In_Config()
    {
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithEndpoint("E1"))
            .BuildAssembly();
        TestApplicationConfig config = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .BuildConfig(assembly);

        AuthorizationFilterContext context = GetContext(assembly, config, "C1", "E1", "incorrect-api-key-value");
        AuthorizeApiKeyAttribute attribute = GetApiKeyAttribute(context.HttpContext);

        await attribute.OnAuthorizationAsync(context);

        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Returns_403_When_Key_Valid_But_Name_Not_Allowed_On_Endpoint()
    {
        string k2 = new('x', 32);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithEndpoint("E1", e1 => e1
                    .WithApiKeyAuthorization("K1"))
                .WithEndpoint("E2", e2 => e2
                    .WithApiKeyAuthorization("K2")))
            .BuildAssembly();
        TestApplicationConfig config = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .WithApiKey("K2", k2)
            .BuildConfig(assembly);

        AuthorizationFilterContext context = GetContext(assembly, config, "C1", "E1", k2);
        AuthorizeApiKeyAttribute attribute = GetApiKeyAttribute(context.HttpContext);

        await attribute.OnAuthorizationAsync(context);

        ObjectResult result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Succeeds_When_Key_Valid_And_Name_Allowed_On_Endpoint()
    {
        string k2 = new('x', 32);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithEndpoint("E1", e1 => e1
                    .WithApiKeyAuthorization("K1")
                    .WithApiKeyAuthorization("K2")))
            .BuildAssembly();
        TestApplicationConfig config = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .WithApiKey("K2", k2)
            .BuildConfig(assembly);

        AuthorizationFilterContext context = GetContext(assembly, config, "C1", "E1", k2);
        AuthorizeApiKeyAttribute attribute = GetApiKeyAttribute(context.HttpContext);

        await attribute.OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task Is_Idempotent_When_Processed_Flag_Is_Set()
    {
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithEndpoint("E1", e1 => e1
                    .WithApiKeyAuthorization("K1")))
            .BuildAssembly();
        TestApplicationConfig config = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .BuildConfig(assembly);

        AuthorizationFilterContext context = GetContext(assembly, config, "C1", "E1");
        AuthorizeApiKeyAttribute attribute = GetApiKeyAttribute(context.HttpContext);

        context.HttpContext.Items["Umea.AuthorizeApiKey.IsProcessed"] = true;
        context.Result = new StatusCodeResult(418);

        await attribute.OnAuthorizationAsync(context);

        StatusCodeResult result = context.Result.ShouldBeOfType<StatusCodeResult>();
        result.StatusCode.ShouldBe(418);
    }

    private static AuthorizationFilterContext GetContext(
        Assembly assembly,
        ApplicationConfigBase config,
        string controllerName,
        string actionName,
        string? apiKeyValue = null)
    {
        Type? controllerType = assembly
            .GetTypes()
            .FirstOrDefault(t =>
                typeof(ControllerBase).IsAssignableFrom(t) &&
                string.Equals(t.Name, controllerName, StringComparison.Ordinal));

        if (controllerType is null)
        {
            throw new InvalidOperationException($"Controller '{controllerName}' not found.");
        }

        MethodInfo? action = controllerType.GetMethod(
            actionName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        if (action is null)
        {
            throw new InvalidOperationException($"Endpoint '{actionName}' not found on {controllerName}.");
        }

        ServiceCollection services = [];
        services
            .AddSingleton(config)
            .AddScoped<IApiKeyAuthorizer, ApiKeyAuthorizer>()
            .AddSingleton<ILogger<AuthorizeApiKeyAttribute>>(_ => NullLogger<AuthorizeApiKeyAttribute>.Instance);
        ServiceProvider provider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = provider,
        };

        if (!string.IsNullOrWhiteSpace(apiKeyValue))
        {
            httpContext.Request.Headers[Header] = apiKeyValue;
        }

        ControllerActionDescriptor controllerActionDescriptor = new()
        {
            ControllerName = controllerType.Name,
            ActionName = action.Name,
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = action,
        };

        object[] metadata = controllerType
            .GetCustomAttributes(inherit: true)
            .Concat(action.GetCustomAttributes(inherit: true))
            .Append(controllerActionDescriptor)
            .ToArray();

        Endpoint endpoint = new(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(metadata),
            displayName: "TestEndpoint");

        httpContext.SetEndpoint(endpoint);

        ActionContext actionContext = new(httpContext, new RouteData(), controllerActionDescriptor);
        AuthorizationFilterContext context = new(actionContext, []);

        return context;
    }

    private static AuthorizeApiKeyAttribute GetApiKeyAttribute(HttpContext context)
    {
        IEnumerable<AuthorizeApiKeyAttribute>? attributes = context
            .GetEndpoint()?.Metadata
            .OfType<AuthorizeApiKeyAttribute>();

        AuthorizeApiKeyAttribute? attribute = attributes?.First(a =>
            a.GetType() == typeof(AuthorizeApiKeyAttribute));

        ArgumentNullException.ThrowIfNull(attribute);

        return attribute;
    }
}
