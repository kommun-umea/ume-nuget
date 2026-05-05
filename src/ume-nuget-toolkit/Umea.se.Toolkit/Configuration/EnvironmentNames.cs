namespace Umea.se.Toolkit.Configuration;

/// <summary>
/// Environment name definitions for our different system types.
/// </summary>
public class EnvironmentNames
{
    public const string UnitTests = "unittests";

    public class Local
    {
        public const string Development = "Development";
    }

    public class Cloud
    {
        public const string Dev = "dev";
        public const string Test = "test";
        public const string Prod = "prod";
    }

    public class OnPrem
    {
        public const string Dev = "dev";
        public const string Test = "test";
        public const string Prod = "prod";
    }
}
