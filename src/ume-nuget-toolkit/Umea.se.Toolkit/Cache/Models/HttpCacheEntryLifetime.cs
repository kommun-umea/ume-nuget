namespace Umea.se.Toolkit.Cache.Models;

public class HttpCacheEntryLifetime
{
    /// <summary>
    /// Maximum lifetime of cache entry.
    /// </summary>
    public TimeSpan? Maximum { get; init; }

    /// <summary>
    /// Lifetime that resets on each access.
    /// </summary>
    public TimeSpan? Sliding { get; init; }
}
