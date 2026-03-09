using Shouldly;
using Umea.se.Toolkit.Cache.Models;

namespace Umea.se.Toolkit.Test.Cache;

public partial class HttpCacheManagerTests
{
    // Confirms per-request lifetimes override the global defaults when provided.
    [Fact]
    public async Task SendRequestWithCache_ExplicitLifetime_OverridesDefaultOptions()
    {
        // Arrange: Global default is LONG (1 hour)
        using HttpCacheManagerTestContext context = new(new HttpCacheOptions
        {
            DefaultCacheEntryLifetime = new() { Maximum = TimeSpan.FromHours(1) },
            MaxJitterOffsetPercentage = 0
        });

        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;
        Task<string?> httpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>("data");
        }

        // Override: Specific request is SHORT (50ms)
        HttpCacheEntryLifetime shortLifetime = new()
        {
            Maximum = TimeSpan.FromMilliseconds(50),
            Sliding = TimeSpan.FromMilliseconds(50)
        };

        // Act 1: Cache it with short lifetime
        await context.HttpCache.SendRequestWithCache(cacheKey, shortLifetime, httpRequest);

        // Act 2: Wait for short lifetime to expire (but well before global default)
        await Task.Delay(500);
        await context.HttpCache.SendRequestWithCache(cacheKey, shortLifetime, httpRequest);

        // Assert: Should have expired and re-invoked
        invocationCount.ShouldBe(2);
    }

    // Verifies cache size pressure triggers compaction under configured limits.
    [Fact]
    public async Task SendRequestWithCache_ExceedsSizeLimit_CompactsCache()
    {
        using HttpCacheManagerTestContext context = new(new HttpCacheOptions
        {
            SizeLimit = 10,
            CompactionPercentage = 0.5
        });

        for (int i = 0; i < 15; i++)
        {
            HttpCacheKey key = context.CreateKey(requestUrl: $"https://service/{i}");
            await context.HttpCache.SendRequestWithCache(key, null, () => Task.FromResult((string?)$"data-{i}"));
        }

        context.HttpCache.GetCount().ShouldBeLessThan(15);
    }

    // Ensures clearing the cache both removes entries and allows them to be repopulated.
    [Fact]
    public async Task Clear_RemovesAllEntriesAndAllowsNewRequests()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        Task<string?> HttpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>("payload");
        }

        string? firstResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);
        invocationCount.ShouldBe(1);
        context.HttpCache.GetCount().ShouldBe(1);

        context.HttpCache.Clear().ShouldBe(1);
        context.HttpCache.GetCount().ShouldBe(0);

        string? secondResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        invocationCount.ShouldBe(2);
        secondResult.ShouldBe(firstResult);
    }

    // Validates that operations against a disposed manager throw as expected.
    [Fact]
    public async Task SendRequestWithCache_OnDisposedManager_Throws()
    {
        HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();
        context.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(() =>
            context.HttpCache.SendRequestWithCache(cacheKey, null, () => Task.FromResult<string?>("payload")));
    }

    // Demonstrates absolute expiration removes cached entries after the specified duration.
    [Fact]
    public async Task SendRequestWithCache_WithAbsoluteExpiration_ExpiresAfterTimeLimit()
    {
        using HttpCacheManagerTestContext context = new(new HttpCacheOptions
        {
            DefaultCacheEntryLifetime = new()
            {
                Maximum = TimeSpan.FromSeconds(10),
                Sliding = TimeSpan.FromMilliseconds(200)
            },
            MaxJitterOffsetPercentage = 0
        });

        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        Task<string?> HttpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>($"payload-{invocationCount}");
        }

        string? first = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        first.ShouldBe("payload-1");
        invocationCount.ShouldBe(1);

        await Task.Delay(50);

        string? second = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        second.ShouldBe("payload-1");
        invocationCount.ShouldBe(1);

        await Task.Delay(1_100);

        string? third = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        third.ShouldBe("payload-2");
        invocationCount.ShouldBe(2);
    }

    // Shows sliding expiration keeps entries alive while access occurs within the window.
    [Fact]
    public async Task SendRequestWithCache_WithSlidingExpiration_IsExtendedByAccess()
    {
        using HttpCacheManagerTestContext context = new(new HttpCacheOptions
        {
            DefaultCacheEntryLifetime = new()
            {
                Maximum = TimeSpan.FromSeconds(10),
                Sliding = TimeSpan.FromMilliseconds(100)
            },
            MaxJitterOffsetPercentage = 0
        });

        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        Task<string?> HttpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>($"payload-{invocationCount}");
        }

        await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        invocationCount.ShouldBe(1);

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(50);
            string? result = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

            result.ShouldBe("payload-1");
            invocationCount.ShouldBe(1);
        }

        await Task.Delay(200);

        string? expiredResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        expiredResult.ShouldBe("payload-2");
        invocationCount.ShouldBe(2);
    }

    // Ensures null lifetimes fall back to the default cache configuration.
    [Fact]
    public async Task SendRequestWithCache_WithNullLifetime_UsesDefaultOptions()
    {
        using HttpCacheManagerTestContext context = new(new HttpCacheOptions
        {
            DefaultCacheEntryLifetime = new()
            {
                Maximum = TimeSpan.FromMilliseconds(100),
                Sliding = TimeSpan.FromMilliseconds(200)
            },
            MaxJitterOffsetPercentage = 0
        });

        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        Task<string?> HttpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>($"payload-{invocationCount}");
        }

        string? first = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        first.ShouldBe("payload-1");
        invocationCount.ShouldBe(1);

        await Task.Delay(1_100);

        string? second = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        second.ShouldBe("payload-2");
        invocationCount.ShouldBe(2);
    }

    // Verifies that completely unset lifetimes leave cache entries without expiration.
    [Fact]
    public async Task SendRequestWithCache_WithNoConfiguredLifetime_DoesNotExpire()
    {
        using HttpCacheManagerTestContext context = new(new HttpCacheOptions
        {
            DefaultCacheEntryLifetime = new HttpCacheEntryLifetime(),
            MaxJitterOffsetPercentage = 0
        });

        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        Task<string?> HttpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>("persistent");
        }

        string? first = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        first.ShouldBe("persistent");
        invocationCount.ShouldBe(1);

        await Task.Delay(1_100);

        string? second = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        second.ShouldBe("persistent");
        invocationCount.ShouldBe(1);
        context.HttpCache.GetCount().ShouldBe(1);
    }
}
