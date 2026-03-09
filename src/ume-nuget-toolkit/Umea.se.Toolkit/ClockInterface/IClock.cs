namespace Umea.se.Toolkit.ClockInterface;

/// <summary>
/// Clock for retrieving the current date and time
/// </summary>
public interface IClock
{
    /// <summary>
    /// Wraps UTCNow
    /// </summary>
    /// <returns>Current UTC DateTime</returns>
    DateTime NowUtc();

    /// <summary>
    /// Wraps UTCNow as DateOnly
    /// </summary>
    /// <returns>Current UTC DateOnly</returns>
    DateOnly TodayUtc();
}
