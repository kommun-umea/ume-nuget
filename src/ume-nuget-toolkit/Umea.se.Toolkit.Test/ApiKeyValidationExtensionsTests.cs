using System.Reflection;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Umea.se.Toolkit.Auth.Exceptions;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Test.Infrastructure;

namespace Umea.se.Toolkit.Test;

public sealed class ApiKeyValidationExtensionsTests
{
    [Fact]
    public void ValidateApiKeys_AllGood_DoesNotThrow()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .WithApiKey("K2", 33);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithEndpoint("E1"))
            .WithController("C2", c2 => c2
                .WithEndpoint("E2", e2 => e2
                    .WithApiKeyAuthorization("K2")))
            .BuildAssembly();

        Should.NotThrow(() => configurationBuilder.BuildConfig(assembly));
    }

    [Fact]
    public void ValidateApiKeys_NoUsages_NoConfiguredKeys_DoesNotThrow()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder();
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1")
            .BuildAssembly();

        Should.NotThrow(() => configurationBuilder.BuildConfig(assembly));
    }

    [Fact]
    public void ValidateApiKeys_UsedKeyMissing_Throws()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .WithApiKey("K2", 33);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithApiKeyAuthorization("K2")
                .WithApiKeyAuthorization("K3"))
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(() => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("used in the code but not configured");
        e.Message.ShouldContain("K3");
        e.Message.ShouldNotContain("K1");
        e.Message.ShouldNotContain("K2");
    }

    [Fact]
    public void ValidateApiKeys_ConfiguredButUnused_Throws()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .WithApiKey("K2", 33)
            .WithApiKey("K4", 34);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithApiKeyAuthorization("K2"))
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(() => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("configured but not used");
        e.Message.ShouldContain("K4");
        e.Message.ShouldNotContain("K1");
        e.Message.ShouldNotContain("K2");
    }

    [Fact]
    public void ValidateApiKeys_EmptyValues_Throws()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 0)
            .WithApiKey("K2", 32, ' ')
            .WithApiKey("K3", 32);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithApiKeyAuthorization("K2")
                .WithApiKeyAuthorization("K3"))
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(() => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("empty");
        e.Message.ShouldContain("K1");
        e.Message.ShouldContain("K2");
        e.Message.ShouldNotContain("K3");
    }

    [Fact]
    public void ValidateApiKeys_DuplicateValues_Throws()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("K1", 32)
            .WithApiKey("K2", 32)
            .WithApiKey("K3", 33);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithApiKeyAuthorization("K2")
                .WithApiKeyAuthorization("K3"))
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(() => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("duplicate values");
        e.Message.ShouldContain("K1");
        e.Message.ShouldContain("K2");
        e.Message.ShouldNotContain("K3");
    }

    [Fact]
    public void ValidateApiKeys_ShortKeys_WhenNotDevelopment_Throws()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithEnvironment(EnvironmentNames.Cloud.Dev)
            .WithApiKey("K1", 31)
            .WithApiKey("K2", 32)
            .WithApiKey("K3", 33);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithApiKeyAuthorization("K2")
                .WithApiKeyAuthorization("K3"))
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(() => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("shorter than the minimum length");
        e.Message.ShouldContain("K1");
        e.Message.ShouldNotContain("K2");
        e.Message.ShouldNotContain("K3");
    }

    [Fact]
    public void ValidateApiKeys_ShortKeys_WhenDevelopment_DoesNotThrow()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithEnvironment(EnvironmentNames.Local.Development)
            .WithApiKey("K1", 31)
            .WithApiKey("K2", 32)
            .WithApiKey("K3", 33);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("K1")
                .WithApiKeyAuthorization("K2")
                .WithApiKeyAuthorization("K3"))
            .BuildAssembly();

        Should.NotThrow(() => configurationBuilder.BuildConfig(assembly));
    }

    #region Legacy Api:Key Backwards Compatibility

    [Fact]
    public void ValidateApiKeys_LegacyApiKey_MigratesToDefault()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithLegacyApiKey(32);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("Default"))
            .BuildAssembly();

        TestApplicationConfig config = Should.NotThrow(() => configurationBuilder.BuildConfig(assembly));

        config.ApiKeys.ShouldContainKey("Default");
        config.ApiKeys["Default"].Length.ShouldBe(32);
    }

    [Fact]
    public void ValidateApiKeys_LegacyApiKey_WorksWithDefaultKeyName()
    {
        // When using legacy Api:Key, the [AuthorizeApiKey("Default")] attribute
        // should work because the legacy key is migrated to "Default"
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithLegacyApiKey(32);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("Default"))
            .BuildAssembly();

        Should.NotThrow(() => configurationBuilder.BuildConfig(assembly));
    }

    [Fact]
    public void ValidateApiKeys_BothLegacyAndNewFormat_Throws()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithLegacyApiKey(32)
            .WithApiKey("Admin", 32);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("Default")
                .WithApiKeyAuthorization("Admin"))
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(
            () => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("both 'Api:Key' and 'Api:Keys' are configured");
    }

    [Fact]
    public void ValidateApiKeys_OnlyNewFormat_DoesNotThrow()
    {
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithApiKey("Default", 32)
            .WithApiKey("Admin", 32, '1');
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("Default")
                .WithApiKeyAuthorization("Admin"))
            .BuildAssembly();

        Should.NotThrow(() => configurationBuilder.BuildConfig(assembly));
    }

    [Fact]
    public void ValidateApiKeys_EmptyLegacyKeyAndNewFormat_Throws()
    {
        // Empty Api:Key still counts as "exists" - should conflict with Api:Keys
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithLegacyApiKey("")
            .WithApiKey("Admin", 32);
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1", c1 => c1
                .WithApiKeyAuthorization("Admin"))
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(
            () => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("both 'Api:Key' and 'Api:Keys' are configured");
    }

    [Fact]
    public void ValidateApiKeys_LegacyKeyAndEmptyNewFormat_Throws()
    {
        // Empty Api:Keys section still counts as "exists" - should conflict with Api:Key
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithLegacyApiKey(32)
            .WithConfiguration("Api:Keys:Placeholder", null); // Creates section but no valid entries
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .BuildAssembly();

        ApiKeyValidationException e = Should.Throw<ApiKeyValidationException>(
            () => configurationBuilder.BuildConfig(assembly));
        e.Message.ShouldContain("both 'Api:Key' and 'Api:Keys' are configured");
    }

    [Fact]
    public void ValidateApiKeys_EmptyLegacyKeyOnly_ReturnsEmpty()
    {
        // Empty Api:Key with no Api:Keys should return empty (not usable)
        ConfigurationBuilder configurationBuilder = TestApplicationConfigBuilder
            .CreateBuilder()
            .WithLegacyApiKey("");
        Assembly assembly = TestAssemblyBuilder
            .CreateBuilder()
            .WithController("C1")
            .BuildAssembly();

        TestApplicationConfig config = Should.NotThrow(() => configurationBuilder.BuildConfig(assembly));
        config.ApiKeys.ShouldBeEmpty();
    }

    #endregion
}
