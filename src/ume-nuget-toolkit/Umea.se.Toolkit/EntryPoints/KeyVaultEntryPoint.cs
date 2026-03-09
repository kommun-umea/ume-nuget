using System.Security.Cryptography.X509Certificates;
using Umea.se.Toolkit.KeyVault;

namespace Umea.se.Toolkit.EntryPoints;

public static class KeyVaultEntryPoint
{
    /// <summary>
    /// Retrieve a certificate from Key Vault.
    /// </summary>
    public static X509Certificate2 GetCertificate(string certificateName)
    {
        return KeyVaultService.GetCertificate(certificateName);
    }
}
