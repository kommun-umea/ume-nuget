namespace Umea.se.Toolkit.ExternalService;

public class CannotFindCertificateException(string thumbPrint)
    : Exception($"Cannot load certificate for thumbprint {thumbPrint}")
{ }
