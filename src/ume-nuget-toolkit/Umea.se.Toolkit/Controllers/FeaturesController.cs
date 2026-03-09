using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.Toolkit.Controllers;

[Produces("application/json")]
[Route(ApiRoutesBase.Features)]
internal class FeaturesController(IVariantFeatureManager featureManager, IFeatureDefinitionProvider definitionProvider) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List enabled feature flags", Description = "Returns an array of enabled feature flag names.")]
    public async Task<IEnumerable<string>> GetEnabledFeatures()
    {
        List<string> enabled = [];
        await foreach (FeatureDefinition definition in definitionProvider.GetAllFeatureDefinitionsAsync())
        {
            if (await featureManager.IsEnabledAsync(definition.Name))
            {
                enabled.Add(definition.Name);
            }
        }

        return enabled;
    }
}
