namespace Umea.se.Toolkit.EntryPoints;

/// <summary>
/// Options used to configure HttpClient BaseAddress, Certificate, X-Api-Key and default headers.
/// </summary>
public class HttpClientOptions
{
    public string? BaseAddress { get; set; }
    public string? XApiKey { get; set; }
    public string? CertificateName { get; set; }
    public IDictionary<string, string> DefaultRequestHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
