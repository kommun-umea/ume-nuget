namespace Umea.se.Toolkit.Configuration.Exceptions;

public class ConfigurationNotFoundException(string key) : Exception($"Configuration [{key}] was not found");
