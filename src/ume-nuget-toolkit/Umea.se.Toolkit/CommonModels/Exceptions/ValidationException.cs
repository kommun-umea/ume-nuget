namespace Umea.se.Toolkit.CommonModels.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    { }
}
