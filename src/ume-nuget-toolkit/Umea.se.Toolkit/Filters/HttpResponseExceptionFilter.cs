using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.Filters;

public class HttpResponseExceptionFilter : IExceptionFilter
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ApplicationConfigBase _config;

    public HttpResponseExceptionFilter(ILoggerFactory loggerFactory, ApplicationConfigBase config)
    {
        _loggerFactory = loggerFactory;
        _config = config;
    }

    public void OnException(ExceptionContext context)
    {
        string source = GetExceptionContextSourceName(context);
        ILogger logger = _loggerFactory.CreateLogger(source);

        switch (context.Exception)
        {
            case HttpResponseException exception:
                logger.LogInformation(
                    "{Source} HttpCode {StatusCode} {Message}",
                    _config.ApiName,
                    (int)exception.StatusCode,
                    exception.ResponseBody.Message ?? exception.ResponseBody.StatusCode);

                SetResultOrMarkHandled(context, GetObjectResult(exception));
                break;

            case OperationCanceledException when context.HttpContext.RequestAborted.IsCancellationRequested:
                logger.LogDebug("Request cancelled by client: {Path}", context.HttpContext.Request.Path);
                context.ExceptionHandled = true;
                break;

            default:
                logger.LogError(context.Exception, context.Exception.Message);

                HttpResponseException exceptionResponse = _config.IsEnvironmentSafe
                    ? HttpResponseFactoryBase.InternalServerError_WithDetails(context.Exception)
                    : HttpResponseFactoryBase.InternalServerError();

                SetResultOrMarkHandled(context, GetObjectResult(exceptionResponse));
                break;
        }
    }

    private static string GetExceptionContextSourceName(ExceptionContext context)
    {
        string? sourceName = context.Exception.TargetSite?.DeclaringType?.FullName;

        if (sourceName is null && context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
        {
            sourceName = actionDescriptor.ControllerTypeInfo.FullName;
        }

        return sourceName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownSource";
    }

    private static void SetResultOrMarkHandled(ExceptionContext context, IActionResult result)
    {
        if (context.HttpContext.Response.HasStarted)
        {
            context.ExceptionHandled = true;
        }
        else
        {
            context.Result = result;
        }
    }

    private static ObjectResult GetObjectResult(HttpResponseException httpResponseException)
    {
        return new ObjectResult(httpResponseException.ResponseBody)
        {
            StatusCode = (int)httpResponseException.StatusCode,
        };
    }
}
