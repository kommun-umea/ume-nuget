using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Cache.Models;
using Umea.se.Toolkit.Logging;

namespace Umea.se.Toolkit.Cache;

internal class HttpCacheLoggingHelper
{
    private readonly ILogger<HttpCacheManager> _logger;

    internal HttpCacheLoggingHelper(ILogger<HttpCacheManager> logger)
    {
        _logger = logger;
    }

    internal void LogCacheHit(HttpCacheKey cacheKey)
    {
        _logger.LogCustomEvent(HttpCacheLoggingConstants.CacheHit, options =>
            {
                options
                    .WithProperty(HttpCacheLoggingConstants.HttpClient, cacheKey.HttpClientName)
                    .WithProperty(HttpCacheLoggingConstants.Url, cacheKey.RequestUrl);
            }
        );
    }

    internal void LogCacheMiss(HttpCacheKey cacheKey)
    {
        _logger.LogCustomEvent(HttpCacheLoggingConstants.CacheMiss, options => options
            .WithProperty(HttpCacheLoggingConstants.HttpClient, cacheKey.HttpClientName)
            .WithProperty(HttpCacheLoggingConstants.Url, cacheKey.RequestUrl)
        );
    }

    internal void LogCacheSet(HttpCacheKey cacheKey, MemoryCacheEntryOptions cacheEntryOptions, int cacheCount)
    {
        _logger.LogCustomEvent(HttpCacheLoggingConstants.CacheSet, options =>
            {
                options
                    .WithProperty(HttpCacheLoggingConstants.HttpClient, cacheKey.HttpClientName)
                    .WithProperty(HttpCacheLoggingConstants.Url, cacheKey.RequestUrl)
                    .WithProperty(HttpCacheLoggingConstants.Entries, cacheCount.ToString(CultureInfo.InvariantCulture))
                    ;

                string? entryLifetimeMaximum = cacheEntryOptions.AbsoluteExpirationRelativeToNow?.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
                if (entryLifetimeMaximum is not null)
                {
                    options.WithProperty(HttpCacheLoggingConstants.MaximumLifetime, entryLifetimeMaximum);
                }

                string? entryLifetimeSliding = cacheEntryOptions.SlidingExpiration?.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
                if (entryLifetimeSliding is not null)
                {
                    options.WithProperty(HttpCacheLoggingConstants.SlidingLifetime, entryLifetimeSliding);
                }
            }
        );
    }

    public void LogCacheManuallyCleared(int entryCount)
    {
        _logger.LogCustomEvent(HttpCacheLoggingConstants.CacheManuallyCleared, options => options
            .WithProperty(HttpCacheLoggingConstants.EntriesCleared, entryCount.ToString(CultureInfo.InvariantCulture))
        );
    }
}
