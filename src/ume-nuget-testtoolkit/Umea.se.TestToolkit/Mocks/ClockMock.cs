using Umea.se.Toolkit.ClockInterface;

namespace Umea.se.TestToolkit.Mocks;

public class ClockMock : IClock
{
    public DateTime MockedNow { private get; set; }

    public DateTime NowUtc()
    {
        return MockedNow;
    }

    public DateOnly TodayUtc()
    {
        return DateOnly.FromDateTime(MockedNow);
    }
}
