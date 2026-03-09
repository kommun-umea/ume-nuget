using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace Umea.se.Toolkit.FeatureFlags;

internal class SimpleFeatureDefinitionProvider(IConfiguration configuration, string sectionName) : IFeatureDefinitionProvider
{
    private readonly Dictionary<string, FeatureDefinition> _features = ParseFeatures(configuration, sectionName)
        .ToDictionary(f => f.Name);

    public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
    {
        _features.TryGetValue(featureName, out FeatureDefinition? definition);
        return Task.FromResult(definition!);
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        foreach (FeatureDefinition definition in _features.Values)
        {
            yield return definition;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static List<FeatureDefinition> ParseFeatures(IConfiguration configuration, string sectionName)
    {
        List<string> names = [];

        // Case 1: Comma-separated string (KeyVault / DevOps variable)
        // e.g. Features = "ErrorReport,EstateService,ContactPersons"
        string? raw = configuration[sectionName];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            names.AddRange(raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        else
        {
            // Case 2: JSON array in appsettings.json
            // e.g. "Features": ["ErrorReport", "EstateService"]
            IConfigurationSection section = configuration.GetSection(sectionName);
            names.AddRange(section.GetChildren().Select(c => c.Value!).Where(v => v is not null));
        }

        return [.. names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new FeatureDefinition { Name = name, EnabledFor = [new FeatureFilterConfiguration { Name = "AlwaysOn" }] })];
    }
}
