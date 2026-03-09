using System.Reflection;
using Microsoft.Extensions.Configuration;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.Test.Infrastructure;

public sealed class TestApplicationConfig(IConfiguration configuration, Assembly? entryAssembly = null) : ApplicationConfigBase(configuration, entryAssembly)
{
    public override bool IsEnvironmentSafe => true;
}
