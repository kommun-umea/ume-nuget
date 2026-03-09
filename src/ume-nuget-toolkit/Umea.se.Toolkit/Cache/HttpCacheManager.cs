using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Cache.Models;

namespace Umea.se.Toolkit.Cache;

public class HttpCacheManager : IDisposable
{
    private readonly HttpCacheLoggingHelper _loggingHelper;
    private readonly HttpCacheOptions _options;
    private readonly Random _jitterRandomizer;
    private readonly MemoryCache _cache;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private bool _isCacheDisposed;

    public HttpCacheManager(ILogger<HttpCacheManager> logger, HttpCacheOptions options)
    {
        _loggingHelper = new HttpCacheLoggingHelper(logger);
        _options = options;
        _jitterRandomizer = Random.Shared; // thread-safe
        MemoryCacheOptions cacheOptions = new()
        {
            CompactionPercentage = options.CompactionPercentage,
            ExpirationScanFrequency = options.ExpirationScanFrequency,
            SizeLimit = options.SizeLimit,
        };
        _cache = new MemoryCache(cacheOptions);
        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };
    }

    public void Dispose()
    {
        if (_isCacheDisposed)
        {
            return;
        }

        _cache.Dispose();

        _isCacheDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get request identifier used as cache key.
    /// </summary>
    internal HttpCacheKey GetHttpCacheKey(string httpClientName, string requestUrl, object? requestBody = null, IReadOnlyDictionary<string, string>? headers = null)
    {
        return new HttpCacheKey(httpClientName, requestUrl, HashObject(requestBody), HashObject(headers));
    }

    /// <summary>
    /// Send http request with response caching.
    /// </summary>
    internal async Task<T?> SendRequestWithCache<T>(HttpCacheKey cacheKey, HttpCacheEntryLifetime? entryLifetime, Func<Task<T?>> httpRequestLambda)
    {
        if (_cache.TryGetValue(cacheKey, out Task<T?>? existing) && existing is not null)
        {
            _loggingHelper.LogCacheHit(cacheKey);
            return await existing.ConfigureAwait(false);
        }

        MemoryCacheEntryOptions cacheEntryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = GetLifetimeWithJitter(entryLifetime?.Maximum, _options.DefaultCacheEntryLifetime.Maximum),
            SlidingExpiration = GetLifetimeWithJitter(entryLifetime?.Sliding, _options.DefaultCacheEntryLifetime.Sliding),
            Size = 1,
        };

        Task<T?> created = _cache.GetOrCreate(cacheKey, entry =>
        {
            _loggingHelper.LogCacheMiss(cacheKey);
            entry.SetOptions(cacheEntryOptions);
            return httpRequestLambda();
        })!;

        try
        {
            T? result = await created.ConfigureAwait(false);
            _loggingHelper.LogCacheSet(cacheKey, cacheEntryOptions, _cache.Count);
            return result;
        }
        catch
        {
            _cache.Remove(cacheKey);
            throw;
        }
    }

    /// <summary>
    /// Get count of current entries in cache.
    /// </summary>
    /// <returns></returns>
    internal int GetCount() => _cache.Count;

    /// <summary>
    /// Clear all entries in cache.
    /// </summary>
    /// <returns></returns>
    internal int Clear()
    {
        int entryCount = GetCount();
        _cache.Compact(1);

        _loggingHelper.LogCacheManuallyCleared(entryCount);
        return entryCount;
    }

    private string HashObject(object? @object)
    {
        if (@object is null)
        {
            return "-";
        }

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(@object, _jsonSerializerOptions);
        return Convert.ToHexString(SHA256.HashData(json));
    }

    /// <summary>
    /// Add random lifetime offset based on <see cref="HttpCacheOptions.MaxJitterOffsetPercentage"/> to entry lifetime to avoid stampedes.
    /// </summary>
    private TimeSpan? GetLifetimeWithJitter(TimeSpan? lifetime, TimeSpan? defaultLifetime)
    {
        lifetime ??= defaultLifetime;
        if (lifetime is null)
        {
            return null;
        }

        double factor = 1.0 + (_jitterRandomizer.NextDouble() * 2 - 1) * _options.MaxJitterOffsetPercentage;
        double seconds = Math.Max(0.001, lifetime.Value.TotalSeconds * factor);
        return TimeSpan.FromSeconds(seconds);
    }
}
