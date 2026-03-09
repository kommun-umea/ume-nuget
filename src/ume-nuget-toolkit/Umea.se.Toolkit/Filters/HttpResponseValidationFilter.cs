using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.Filters;

public class HttpResponseValidationFilter : IActionFilter
{
    private readonly ILogger<HttpResponseValidationFilter> _logger;
    private readonly ApplicationConfigBase _config;
    private const string DefaultErrorMessage = "Input validation failed for an unknown reason.";

    public HttpResponseValidationFilter(ILogger<HttpResponseValidationFilter> logger, ApplicationConfigBase config)
    {
        _logger = logger;
        _config = config;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        string errorMessage = context.ModelState
            .LastOrDefault()
            .Value?
            .Errors
            .LastOrDefault()?
            .ErrorMessage
            ?? DefaultErrorMessage;

        HttpResponseException exception = HttpResponseFactoryBase.BadRequest(errorMessage);

        _logger.LogInformation(
            "{Source} HttpCode {StatusCode} {Message}",
            _config.ApiTitle,
            (int)exception.StatusCode,
            errorMessage);

        context.Result = new ObjectResult(exception.ResponseBody)
        {
            StatusCode = (int)exception.StatusCode,
        };
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
