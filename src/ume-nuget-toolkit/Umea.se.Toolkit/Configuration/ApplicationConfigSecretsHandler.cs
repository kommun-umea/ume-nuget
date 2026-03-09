using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Umea.se.Toolkit.KeyVault;

namespace Umea.se.Toolkit.Configuration;

internal static partial class ApplicationConfigSecretsHandler
{
    private const string _secretConfigPrefix = "@KeyVault";

    internal static void LoadKeyVaultSecrets(IConfiguration configuration)
    {
        IEnumerable<KeyValuePair<string, string?>> variables = configuration.AsEnumerable();

        foreach (KeyValuePair<string, string?> variable in variables)
        {
            if (variable.Value is null || !variable.Value.StartsWith(_secretConfigPrefix))
            {
                continue;
            }

            Match secretNameMatch = GetKeyVaultSecretNameRegex().Match(variable.Value);
            string secretName = secretNameMatch.Groups[1].Value;
            string secretValue = KeyVaultService.GetSecret(secretName);

            configuration[variable.Key] = secretValue;
        }
    }

    [GeneratedRegex(@$"{_secretConfigPrefix}\(([^)]+)\)")]
    private static partial Regex GetKeyVaultSecretNameRegex();
}
