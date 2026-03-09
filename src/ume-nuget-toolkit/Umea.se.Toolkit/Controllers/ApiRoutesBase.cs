namespace Umea.se.Toolkit.Controllers;

public abstract class ApiRoutesBase
{
    protected const string RoutePrefixV1 = "/api/v1.0";

    public const string Home = $"{RoutePrefixV1}/home";
    public const string Cache = $"{RoutePrefixV1}/cache";
    public const string Features = $"{RoutePrefixV1}/features";
}
