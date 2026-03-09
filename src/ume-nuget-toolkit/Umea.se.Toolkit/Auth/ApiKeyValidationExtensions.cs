using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Umea.se.Toolkit.Auth.Exceptions;
using Umea.se.Toolkit.CommonModels.Exceptions;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.Auth;

public static class ApiKeyValidationExtensions
{
    private const int MinApiKeyLength = 32;

    public static void ValidateApiKeys(this ApplicationConfigBase config, Assembly? entryAssembly = null)
    {
        entryAssembly ??= Assembly.GetEntryAssembly() ?? throw new InvalidEntryAssemblyException();
        List<string> usedKeys = GetUsedKeys(entryAssembly);

        EnsureUsedKeysAreConfigured(config, usedKeys);
        EnsureConfiguredKeysAreUsed(config, usedKeys);
        EnsureNoKeysAreEmpty(config);
        EnsureNoDuplicateKeys(config);
        EnsureKeyLength(config);
    }

    private static List<string> GetUsedKeys(Assembly entryAssembly)
    {
        List<Type> controllers = [.. entryAssembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))];

        IEnumerable<string> usedKeys =
        [
            ..GetControllerUsedKeyNames(controllers),
            ..GetControllerMethodsUsedKeyNames(controllers),
        ];

        return [.. usedKeys.Distinct()];
    }

    private static void EnsureUsedKeysAreConfigured(ApplicationConfigBase config, List<string> usedKeys)
    {
        List<string> unconfiguredKeys = [.. usedKeys.Where(usedKey => !config.ApiKeys.ContainsKey(usedKey))];

        if (unconfiguredKeys.Count > 0)
        {
            string unconfiguredKeysString = string.Join(", ", unconfiguredKeys);
            throw new ApiKeyValidationException("The following API key(s) are used in the code but not configured: " + unconfiguredKeysString);
        }
    }

    private static void EnsureConfiguredKeysAreUsed(ApplicationConfigBase config, List<string> usedKeys)
    {
        List<string> unusedKeys = [.. config.ApiKeys
            .Where(apiKey => !usedKeys.Contains(apiKey.Key))
            .Select(apiKey => apiKey.Key)];

        if (unusedKeys.Count > 0)
        {
            string unusedKeysString = string.Join(", ", unusedKeys);
            throw new ApiKeyValidationException("The following API key(s) are configured but not used in the code: " + unusedKeysString);
        }
    }

    private static void EnsureNoKeysAreEmpty(ApplicationConfigBase config)
    {
        List<string> emptyKeys = [.. config.ApiKeys
            .Where(apiKey => string.IsNullOrWhiteSpace(apiKey.Value))
            .Select(apiKey => apiKey.Key)];

        if (emptyKeys.Count > 0)
        {
            string emptyKeysString = string.Join(", ", emptyKeys);
            throw new ApiKeyValidationException("The following API key(s) are empty: " + emptyKeysString);
        }
    }

    private static void EnsureNoDuplicateKeys(ApplicationConfigBase config)
    {
        List<IEnumerable<string>> duplicateKeyGroups = [.. config.ApiKeys
            .GroupBy(apiKey => apiKey.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Select(apiKey => apiKey.Key))];

        if (duplicateKeyGroups.Count > 0)
        {
            string duplicateKeyGroupsString = string.Join(", ", duplicateKeyGroups.Select(keyGroup => $"[{string.Join(", ", keyGroup)}]"));
            throw new ApiKeyValidationException("The following API keys have duplicate values: " + duplicateKeyGroupsString);
        }
    }

    private static void EnsureKeyLength(ApplicationConfigBase config)
    {
        if (config.Environment is EnvironmentNames.Local.Development)
        {
            return;
        }

        List<string> shortKeys = [.. config.ApiKeys
            .Where(apiKey => apiKey.Value.Length < MinApiKeyLength)
            .Select(apiKey => apiKey.Key)];

        if (shortKeys.Count > 0)
        {
            string shortKeysString = string.Join(", ", shortKeys);
            throw new ApiKeyValidationException($"The following API key(s) are shorter than the minimum length of {MinApiKeyLength} characters: " + shortKeysString);
        }
    }

    private static IEnumerable<string> GetControllerUsedKeyNames(List<Type> controllers)
    {
        return controllers.SelectMany(c => c
            .GetCustomAttributes(typeof(AuthorizeApiKeyAttribute), true)
            .OfType<AuthorizeApiKeyAttribute>()
            .Select(a => a.Name));
    }

    private static IEnumerable<string> GetControllerMethodsUsedKeyNames(List<Type> controllers)
    {
        return controllers.SelectMany(c => c
            .GetMethods()
            .SelectMany(m => m
                .GetCustomAttributes(typeof(AuthorizeApiKeyAttribute), true)
                .OfType<AuthorizeApiKeyAttribute>()
                .Select(a => a.Name)));
    }
}
