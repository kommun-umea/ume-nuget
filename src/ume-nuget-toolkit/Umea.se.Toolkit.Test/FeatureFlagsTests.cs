using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Shouldly;
using Umea.se.Toolkit.EntryPoints;

namespace Umea.se.Toolkit.Test;

public class FeatureFlagsTests
{
    private static IVariantFeatureManager BuildFeatureManager(Dictionary<string, string?> configValues)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddFeatureFlags();

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IVariantFeatureManager>();
    }

    private static IFeatureDefinitionProvider BuildDefinitionProvider(Dictionary<string, string?> configValues)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddFeatureFlags();

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IFeatureDefinitionProvider>();
    }

    [Fact]
    public async Task CommaSeparatedString_EnablesAllListedFeatures()
    {
        IVariantFeatureManager fm = BuildFeatureManager(new()
        {
            ["Features"] = "ErrorReport,EstateService,Documents"
        });

        (await fm.IsEnabledAsync("ErrorReport")).ShouldBeTrue();
        (await fm.IsEnabledAsync("EstateService")).ShouldBeTrue();
        (await fm.IsEnabledAsync("Documents")).ShouldBeTrue();
    }

    [Fact]
    public async Task CommaSeparatedString_UnlistedFeatureIsDisabled()
    {
        IVariantFeatureManager fm = BuildFeatureManager(new()
        {
            ["Features"] = "ErrorReport,Documents"
        });

        (await fm.IsEnabledAsync("EstateService")).ShouldBeFalse();
    }

    [Fact]
    public async Task CommaSeparatedString_TrimsWhitespace()
    {
        IVariantFeatureManager fm = BuildFeatureManager(new()
        {
            ["Features"] = " ErrorReport , Documents "
        });

        (await fm.IsEnabledAsync("ErrorReport")).ShouldBeTrue();
        (await fm.IsEnabledAsync("Documents")).ShouldBeTrue();
    }

    [Fact]
    public async Task JsonArray_EnablesAllListedFeatures()
    {
        IVariantFeatureManager fm = BuildFeatureManager(new()
        {
            ["Features:0"] = "ErrorReport",
            ["Features:1"] = "EstateService",
            ["Features:2"] = "Documents"
        });

        (await fm.IsEnabledAsync("ErrorReport")).ShouldBeTrue();
        (await fm.IsEnabledAsync("EstateService")).ShouldBeTrue();
        (await fm.IsEnabledAsync("Documents")).ShouldBeTrue();
    }

    [Fact]
    public async Task JsonArray_UnlistedFeatureIsDisabled()
    {
        IVariantFeatureManager fm = BuildFeatureManager(new()
        {
            ["Features:0"] = "ErrorReport"
        });

        (await fm.IsEnabledAsync("ContactPersons")).ShouldBeFalse();
    }

    [Fact]
    public async Task EmptyConfig_AllFeaturesDisabled()
    {
        IVariantFeatureManager fm = BuildFeatureManager([]);

        (await fm.IsEnabledAsync("ErrorReport")).ShouldBeFalse();
    }

    [Fact]
    public async Task DuplicateNames_DeduplicatedCaseInsensitive()
    {
        IFeatureDefinitionProvider provider = BuildDefinitionProvider(new()
        {
            ["Features"] = "ErrorReport,errorreport,ERRORREPORT"
        });

        List<FeatureDefinition> definitions = [];
        await foreach (FeatureDefinition definition in provider.GetAllFeatureDefinitionsAsync())
        {
            definitions.Add(definition);
        }

        definitions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CustomSectionName_ReadsFromSpecifiedSection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MyFlags"] = "Alpha,Beta"
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddFeatureFlags("MyFlags");

        ServiceProvider provider = services.BuildServiceProvider();
        IVariantFeatureManager fm = provider.GetRequiredService<IVariantFeatureManager>();

        (await fm.IsEnabledAsync("Alpha")).ShouldBeTrue();
        (await fm.IsEnabledAsync("Beta")).ShouldBeTrue();
    }

    [Fact]
    public async Task StandardSchema_WorksAlongsideSimpleFlags()
    {
        IVariantFeatureManager fm = BuildFeatureManager(new()
        {
            ["Features"] = "ErrorReport,Documents",
            ["feature_management:feature_flags:0:id"] = "BetaDashboard",
            ["feature_management:feature_flags:0:enabled"] = "true"
        });

        (await fm.IsEnabledAsync("ErrorReport")).ShouldBeTrue();
        (await fm.IsEnabledAsync("Documents")).ShouldBeTrue();
        (await fm.IsEnabledAsync("BetaDashboard")).ShouldBeTrue();
    }

    [Fact]
    public async Task StandardSchema_DisabledFlagOverridesSimpleFlag()
    {
        IVariantFeatureManager fm = BuildFeatureManager(new()
        {
            ["Features"] = "ErrorReport,Documents",
            ["feature_management:feature_flags:0:id"] = "ErrorReport",
            ["feature_management:feature_flags:0:enabled"] = "false"
        });

        // Standard schema wins — ErrorReport is disabled
        (await fm.IsEnabledAsync("ErrorReport")).ShouldBeFalse();
        (await fm.IsEnabledAsync("Documents")).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllDefinitions_ReturnsMergedSet()
    {
        IFeatureDefinitionProvider provider = BuildDefinitionProvider(new()
        {
            ["Features"] = "ErrorReport,Documents",
            ["feature_management:feature_flags:0:id"] = "BetaDashboard",
            ["feature_management:feature_flags:0:enabled"] = "true"
        });

        List<string> names = [];
        await foreach (FeatureDefinition definition in provider.GetAllFeatureDefinitionsAsync())
        {
            names.Add(definition.Name);
        }

        names.ShouldContain("ErrorReport");
        names.ShouldContain("Documents");
        names.ShouldContain("BetaDashboard");
        names.Count.ShouldBe(3);
    }
}
