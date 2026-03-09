using System.Text.Json.Serialization;

namespace Umea.se.Toolkit.Logging.OnPremLogger.Models;

public class ExceptionLog : BaseLog
{
    public required string Message { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionMessage { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionStackTrace { get; init; }
}
