using Umea.se.Toolkit.ClockInterface;

namespace Umea.se.Toolkit.Cache;

public class Cache<TKey, TData>
    where TKey : notnull
    where TData : class
{
    private readonly int _size;
    private readonly TimeSpan _cacheValidity;

    private readonly IClock _clock;

    private readonly Dictionary<TKey, (TData info, DateTime added)> _cache;
    private readonly object _lock = new();

    public Cache(IClock clock, int size, TimeSpan cacheValidity)
    {
        _clock = clock;
        _size = size;
        _cacheValidity = cacheValidity;
        _cache = new Dictionary<TKey, (TData, DateTime)>(_size);
    }

    public async Task<TData> GetData(
        TKey key,
        Func<TKey, Task<TData>> getDataFunction)
    {
        lock (_lock)
        {
            TData? dataFromCache = LoadFromCache(key);

            if (dataFromCache != null)
            {
                return dataFromCache;
            }
        }

        TData fetchedData = await getDataFunction(key);

        lock (_lock)
        {
            /*
             * We check the cache again here, since things may have changed since we last checked.
             * While we were calling getDataFunction, the lock was released. (This is important for performance.)
             * During this time, another thread may be ahead of us and may have stored the data.
             * Example:
             *      A enters first lock and sees that the data is not in the cache.
             *      B enters first lock and sees that the data is not in the cache.
             *      A starts fetching the data
             *      B starts fetching the data
             *      A enters second lock and sees that the data is not in the cache and stores the data.
             *      B enters second lock and sees that the data exists and returns it.
             * If we didn't have this check, B would store the data again in the last step.
             */
            TData? dataFromCache = LoadFromCache(key);

            if (dataFromCache != null)
            {
                return dataFromCache;
            }

            AddNewDataToCache(key, fetchedData);
            return fetchedData;
        }
    }

    private void AddNewDataToCache(TKey key, TData fetchedData)
    {
        if (CacheIsFull())
        {
            RemoveOldestEntry();
        }

        if (_cache.ContainsKey(key))
        {
            _cache.Remove(key);
        }

        _cache.Add(key, (fetchedData, _clock.NowUtc()));
    }

    private TData? LoadFromCache(TKey key)
    {
        bool cacheHit = _cache.TryGetValue(key, out (TData info, DateTime added) cachedInfo);

        if (cacheHit && cachedInfo.added.Add(_cacheValidity) > _clock.NowUtc())
        {
            return cachedInfo.info;
        }
        else
        {
            return null;
        }
    }

    private bool CacheIsFull()
    {
        return _cache.Count >= _size;
    }

    private void RemoveOldestEntry()
    {
        TKey oldestEntry = _cache.MinBy(e => e.Value.added).Key;
        _cache.Remove(oldestEntry);
    }

    public void InvalidateCache()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }
}
