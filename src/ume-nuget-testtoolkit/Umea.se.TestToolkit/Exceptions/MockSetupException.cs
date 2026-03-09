namespace Umea.se.TestToolkit.Exceptions;

internal class MockNotSetupException : Exception
{
    internal MockNotSetupException(string message) : base(message)
    { }
    internal MockNotSetupException(string message, Exception innerException) : base(message, innerException)
    { }
}
