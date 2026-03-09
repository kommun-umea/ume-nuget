using System.Net;
using System.Text.Json.Serialization;

namespace Umea.se.Toolkit.Filters;

/// <summary>
/// These special exceptions are meant to be caught by the web framework.
/// They are turned into web-responses by HttpResponseExceptionFilter.
/// They may ONLY be thrown in controllers endpoint methods.
/// </summary>
public class HttpResponseException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ResponseBody ResponseBody { get; }

    /// <inheritdoc />
    public HttpResponseException(HttpStatusCode statusCode, ResponseBody? responseBody = null)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody ?? new ResponseBody();
        ResponseBody.StatusCode = statusCode.ToString();
    }
}

public class ResponseBody
{
    internal ResponseBody(string? message = null, string? stackTrace = null)
    {
        Message = message;
        StackTrace = stackTrace;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusCode { get; internal set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; internal set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; internal set; }
}
