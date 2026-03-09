namespace Umea.se.Toolkit.ExternalService;

public class PingFailedException : Exception
{
    public PingFailedException(string serviceName)
        : base($"Ping unsuccessful at {serviceName}.")
    { }
}
