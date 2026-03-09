namespace Umea.se.Toolkit.Cache.Models;

internal class HttpCacheLoggingConstants
{
    // Event names
    internal const string CacheHit = "CacheHit";
    internal const string CacheMiss = "CacheMiss";
    internal const string CacheSet = "CacheSet";
    internal const string CacheLockGateEnqueued = "CacheLockGateEnqueued";
    internal const string CacheLockGateActivated = "CacheLockGateActivated";
    internal const string CacheLockGateDeactivated = "CacheLockGateDeactivated";
    internal const string CacheLockRemoved = "CacheLockRemoved";
    internal const string CacheManuallyCleared = "CacheManuallyCleared";

    // Property names
    internal const string HttpClient = "HttpClient";
    internal const string Url = "Url";
    internal const string LockId = "LockId";
    internal const string LockQueuePosition = "LockQueuePosition";
    internal const string MaximumLifetime = "MaximumLifetime";
    internal const string SlidingLifetime = "SlidingLifetime";
    internal const string Entries = "Entries";
    internal const string EntriesCleared = "EntriesCleared";
}
