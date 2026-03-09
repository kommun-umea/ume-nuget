using Microsoft.FeatureManagement;

namespace Umea.se.Toolkit.FeatureFlags;

internal class CompositeFeatureDefinitionProvider(IEnumerable<IFeatureDefinitionProvider> providers) : IFeatureDefinitionProvider
{
    private readonly IFeatureDefinitionProvider[] _providers = providers.ToArray();

    public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
    {
        // Last provider wins — standard schema overrides simple flags
        for (int i = _providers.Length - 1; i >= 0; i--)
        {
            FeatureDefinition? definition = await _providers[i].GetFeatureDefinitionAsync(featureName);
            if (definition is not null)
            {
                return definition;
            }
        }

        return null!;
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        Dictionary<string, FeatureDefinition> merged = new(StringComparer.OrdinalIgnoreCase);

        foreach (IFeatureDefinitionProvider provider in _providers)
        {
            await foreach (FeatureDefinition definition in provider.GetAllFeatureDefinitionsAsync())
            {
                // Last provider wins for duplicates
                merged[definition.Name] = definition;
            }
        }

        foreach (FeatureDefinition definition in merged.Values)
        {
            yield return definition;
        }
    }
}
