using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.Controllers;

/// <summary>
/// Base controller containing default health-check endpoint.
/// </summary>
public abstract class HomeControllerBase(ILogger<HomeControllerBase> logger, ApplicationConfigBase config) : ControllerBase
{
    private readonly ILogger<HomeControllerBase> _logger = logger;
    private readonly ApplicationConfigBase _config = config;

    [HttpGet("ping")]
    [SwaggerOperation(Summary = "Health-check endpoint", Description = "Returns 200 OK to show that the service is up.")]
    public string Ping()
    {
        _logger.LogInformation("{ApiTitleWithEnvironment} got pinged...", _config.ApiTitleWithEnvironment);

        return "pong";
    }
}
