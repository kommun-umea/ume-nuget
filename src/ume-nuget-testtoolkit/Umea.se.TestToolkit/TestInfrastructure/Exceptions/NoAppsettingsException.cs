namespace Umea.se.TestToolkit.TestInfrastructure.Exceptions;

using System;

public class NoAppsettingsException(string message) : Exception(message)
{
}
