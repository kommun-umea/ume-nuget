namespace Umea.se.Toolkit.ClockInterface;

/// <inheritdoc />
internal class Clock : IClock
{
    /// <inheritdoc />
    public DateTime NowUtc()
    {
        return DateTime.UtcNow;
    }

    public DateOnly TodayUtc()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
