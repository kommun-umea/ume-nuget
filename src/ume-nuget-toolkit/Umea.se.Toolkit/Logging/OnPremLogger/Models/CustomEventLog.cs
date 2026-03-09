namespace Umea.se.Toolkit.Logging.OnPremLogger.Models;

public class CustomEventLog : BaseLog
{
    public required string EventName { get; init; }
    public Dictionary<string, string>? Properties { get; init; }
    public Dictionary<string, double>? Measurements { get; init; }
}
