using System.Net;

namespace Umea.se.Toolkit.ExternalService;

public class UnsuccessfulHttpCallException : Exception
{
    public HttpStatusCode? StatusCode { get; init; }

    public UnsuccessfulHttpCallException(string message, HttpStatusCode? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public UnsuccessfulHttpCallException(string message, Exception innerException, HttpStatusCode? statusCode = null) :
        base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
