namespace Umea.se.Toolkit.Logging.Models;

public class CustomEventOptions
{
    private readonly Dictionary<string, string> _properties = [];
    private readonly Dictionary<string, double> _measurements = [];

    /// <summary>
    /// Add property to customProperties of a custom event.
    /// </summary>
    public CustomEventOptions WithProperty(string key, string value)
    {
        _properties.Add(key, value);
        return this;
    }

    /// <summary>
    /// Add measurement to customMeasurements of a custom event.
    /// </summary>
    [Obsolete("Measurements are not supported yet. Using this method will store the value in customProperties.")]
    public CustomEventOptions WithMeasurement(string key, double value)
    {
        _measurements.Add(key, value);
        return this;
    }

    /// <summary>
    /// Get customProperties and customMeasurements formatted as a message template string.
    /// <example>", Property1={Property1}, Measurement1={Measurement1}"</example>
    /// </summary>
    internal string GetMessageTemplateString()
    {
        return GetMessageTemplateString(_properties) + GetMessageTemplateString(_measurements);
    }

    /// <summary>
    /// Get values of customProperties and customMeasurements as array of objects.
    /// </summary>
    internal object[] GetValues()
    {
        return
        [
            .._properties.Values,
            .._measurements.Values,
        ];
    }

    private static string GetMessageTemplateString<T>(Dictionary<string, T> dictionary)
    {
        return string.Join(string.Empty, dictionary.Keys.Select(key => $", {key}={{{key}}}"));
    }
}

