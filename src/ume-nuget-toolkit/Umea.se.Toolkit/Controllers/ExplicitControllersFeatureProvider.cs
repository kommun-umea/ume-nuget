using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Umea.se.Toolkit.Controllers;

internal sealed class ExplicitControllersFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly TypeInfo[] _types;

    public ExplicitControllersFeatureProvider(params Type[] controllers)
    {
        _types = [.. controllers.Select(t => t.GetTypeInfo())];
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (TypeInfo type in _types)
        {
            if (!feature.Controllers.Contains(type))
            {
                feature.Controllers.Add(type);
            }
        }
    }
}
