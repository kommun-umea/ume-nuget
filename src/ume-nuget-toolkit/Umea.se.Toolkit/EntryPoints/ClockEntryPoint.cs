using Microsoft.Extensions.DependencyInjection;
using Umea.se.Toolkit.ClockInterface;

namespace Umea.se.Toolkit.EntryPoints;

public static class ClockEntryPoint
{
    /// <summary>
    /// Sets up injection of IClock which provides current Utc time and date.
    /// This is to be used instead of static DateTime methods, so it can be mocked.
    /// The Testing toolkit includes a configurable mock for this interface.
    /// </summary>
    public static IServiceCollection AddClock(this IServiceCollection services)
    {
        return services.AddTransient<IClock, Clock>();
    }
}
