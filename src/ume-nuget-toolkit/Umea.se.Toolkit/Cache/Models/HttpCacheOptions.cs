namespace Umea.se.Toolkit.Cache.Models;

/// <summary>
/// HttpCache configuration options.
/// </summary>
public class HttpCacheOptions
{
    /// <summary>
    /// Maximum size of cache.
    /// <br/>
    /// Default: 25000
    /// </summary>
    public long SizeLimit { get; init; } = 25_000;

    /// <summary>
    /// Percentage of cache entries to remove when <see cref="SizeLimit"/> is reached.
    /// <br/>
    /// Default: 25%
    /// </summary>
    public double CompactionPercentage { get; init; } = 0.25;

    /// <summary>
    /// How often the cache should check if <see cref="SizeLimit"/> is reached and should remove entries in cache.
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Cache entry lifetime.
    /// <br/>
    /// Default maximum cache entry lifetime: 10min
    ///<br/>
    /// Default sliding cache entry lifetime: 2min
    /// </summary>
    public HttpCacheEntryLifetime DefaultCacheEntryLifetime { get; init; } = new()
    {
        Maximum = TimeSpan.FromMinutes(10),
        Sliding = TimeSpan.FromMinutes(2),
    };

    /// <summary>
    /// Max jitter offset to apply to cache entry lifetime with the purpose of avoiding stampedes.
    /// <br/>
    /// Default: +-15%
    /// </summary>
    public double MaxJitterOffsetPercentage { get; init; } = 0.15;
}
