using System.Text.Json.Serialization;

namespace Umea.se.Toolkit.Logging.OnPremLogger.Models;

[JsonDerivedType(typeof(InformationLog))]
[JsonDerivedType(typeof(WarningLog))]
[JsonDerivedType(typeof(ExceptionLog))]
[JsonDerivedType(typeof(CustomEventLog))]
public abstract class BaseLog
{
    public required string Application { get; init; }
    public required string Source { get; init; }
}
