using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Umea.se.Toolkit.Cache;
using Umea.se.Toolkit.Cache.Models;

namespace Umea.se.Toolkit.Test.Cache;

public partial class HttpCacheManagerTests
{
    // Verifies a cache miss triggers the delegate and stores the payload.
    [Fact]
    public async Task SendRequestWithCache_CacheMiss_InvokesLambdaAndCachesValue()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();

        int invocationCount = 0;
        string expectedPayload = Guid.NewGuid().ToString();

        Task<string?> httpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>(expectedPayload);
        }

        string? result = await context.HttpCache.SendRequestWithCache(cacheKey, null, httpRequest);

        invocationCount.ShouldBe(1);
        result.ShouldBe(expectedPayload);
        context.HttpCache.GetCount().ShouldBe(1);
    }

    // Confirms subsequent cache hits avoid rerunning the request delegate.
    [Fact]
    public async Task SendRequestWithCache_CacheHit_SkipsLambdaExecution()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();

        int invocationCount = 0;
        string expectedPayload = Guid.NewGuid().ToString();

        Task<string?> HttpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>(expectedPayload);
        }

        string? firstResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);
        string? secondResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        invocationCount.ShouldBe(1);
        secondResult.ShouldBe(expectedPayload);
        secondResult.ShouldBeSameAs(firstResult);
    }

    // Ensures serialization integrity when caching a complex object graph.
    [Fact]
    public async Task SendRequestWithCache_CachesComplexObjectWithDataIntegrity()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey(body: new { Example = "request" });

        int invocationCount = 0;
        ComplexPayload template = new(Guid.NewGuid().ToString(), new NestedPayload("example", DateTime.UtcNow), [1, 2, 3]);

        Task<ComplexPayload?> HttpRequest()
        {
            invocationCount++;
            ComplexPayload result = template with
            {
                Scores = [.. template.Scores]
            };

            return Task.FromResult<ComplexPayload?>(result);
        }

        ComplexPayload? firstResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);
        ComplexPayload? secondResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);

        invocationCount.ShouldBe(1);
        secondResult.ShouldBe(firstResult);
        secondResult.ShouldNotBeNull();
        secondResult!.Nested.ShouldBe(firstResult!.Nested);
        secondResult.Scores.ShouldBe(firstResult.Scores);
    }

    // Demonstrates that null responses are cached and reused. Should this be an option?
    [Fact]
    public async Task SendRequestWithCache_NullResponse_IsCached()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        Task<string?> httpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>(null);
        }

        string? firstResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, httpRequest);
        string? secondResult = await context.HttpCache.SendRequestWithCache(cacheKey, null, httpRequest);

        invocationCount.ShouldBe(1);
        firstResult.ShouldBeNull();
        secondResult.ShouldBeNull();
    }

    // Confirms repeated disposal calls short-circuit once the cache is disposed.
    [Fact]
    public void Dispose_SubsequentCallsAreNoOps()
    {
        HttpCacheManagerTestContext context = new();
        context.HttpCache.Dispose();
        context.HttpCache.Dispose();
    }

    // Uses the same request descriptor twice to check key equality semantics.
    [Fact]
    public void GetHttpCacheKey_IdenticalRequest_ProducesEqualKey()
    {
        using HttpCacheManagerTestContext context = new();

        object requestBody = new
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            Values = new[] { "a", "b" }
        };

        HttpCacheKey firstKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", requestBody, null);
        HttpCacheKey secondKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", requestBody, null);

        secondKey.ShouldBe(firstKey);
    }

    // Verifies differing bodies produce unique cache keys even with matching metadata.
    [Fact]
    public void GetHttpCacheKey_DifferentBodies_ProducesDistinctKeys()
    {
        using HttpCacheManagerTestContext context = new();

        object firstBody = new
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            Values = new[] { "a", "b" }
        };

        object secondBody = new
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            Values = new[] { "a", "b" }
        };

        HttpCacheKey firstKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", firstBody, null);
        HttpCacheKey secondKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", secondBody, null);

        secondKey.ShouldNotBe(firstKey);
    }

    // Ensures null bodies and empty objects generate distinct cache keys.
    [Fact]
    public void GetHttpCacheKey_NullVsEmptyBody_ProducesDistinctKeys()
    {
        using HttpCacheManagerTestContext context = new();

        HttpCacheKey nullBodyKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", null, null);
        HttpCacheKey emptyBodyKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", new object(), null);

        emptyBodyKey.ShouldNotBe(nullBodyKey);
    }

    // Confirms URL and client name differences are reflected in cache keys.
    [Fact]
    public void GetHttpCacheKey_DifferentUrlOrClient_ProducesDistinctKeys()
    {
        using HttpCacheManagerTestContext context = new();

        HttpCacheKey firstKey = context.HttpCache.GetHttpCacheKey("client-a", "https://service/resource", null, null);
        HttpCacheKey secondKey = context.HttpCache.GetHttpCacheKey("client-b", "https://service/resource", null, null);
        HttpCacheKey thirdKey = context.HttpCache.GetHttpCacheKey("client-a", "https://service/resource-2", null, null);

        secondKey.ShouldNotBe(firstKey);
        thirdKey.ShouldNotBe(firstKey);
        thirdKey.ShouldNotBe(secondKey);
    }

    // Verifies identical headers produce the same cache key.
    [Fact]
    public void GetHttpCacheKey_IdenticalHeaders_ProducesEqualKey()
    {
        using HttpCacheManagerTestContext context = new();

        Dictionary<string, string> headers = new()
        {
            ["Authorization"] = "Bearer token123",
            ["X-Custom-Header"] = "value"
        };

        HttpCacheKey firstKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", null, headers);
        HttpCacheKey secondKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", null, headers);

        secondKey.ShouldBe(firstKey);
    }

    // Ensures different headers produce distinct cache keys.
    [Fact]
    public void GetHttpCacheKey_DifferentHeaders_ProducesDistinctKeys()
    {
        using HttpCacheManagerTestContext context = new();

        Dictionary<string, string> firstHeaders = new()
        {
            ["Authorization"] = "Bearer token123",
            ["X-Custom-Header"] = "value1"
        };

        Dictionary<string, string> secondHeaders = new()
        {
            ["Authorization"] = "Bearer token123",
            ["X-Custom-Header"] = "value2"
        };

        HttpCacheKey firstKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", null, firstHeaders);
        HttpCacheKey secondKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", null, secondHeaders);

        secondKey.ShouldNotBe(firstKey);
    }

    // Confirms null headers and empty headers produce distinct cache keys.
    [Fact]
    public void GetHttpCacheKey_NullVsEmptyHeaders_ProducesDistinctKeys()
    {
        using HttpCacheManagerTestContext context = new();

        HttpCacheKey nullHeadersKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", null, null);
        HttpCacheKey emptyHeadersKey = context.HttpCache.GetHttpCacheKey("api-client", "https://service/resource", null, new Dictionary<string, string>());

        emptyHeadersKey.ShouldNotBe(nullHeadersKey);
    }

    // Verifies cache differentiation when only headers differ.
    [Fact]
    public async Task SendRequestWithCache_DifferentHeaders_CachesDistinctly()
    {
        using HttpCacheManagerTestContext context = new();

        Dictionary<string, string> firstHeaders = new()
        {
            ["X-User-Id"] = "user1"
        };

        Dictionary<string, string> secondHeaders = new()
        {
            ["X-User-Id"] = "user2"
        };

        int invocationCount = 0;

        Task<string?> HttpRequest()
        {
            invocationCount++;
            return Task.FromResult<string?>($"payload-{invocationCount}");
        }

        HttpCacheKey firstKey = context.CreateKey(headers: firstHeaders);
        HttpCacheKey secondKey = context.CreateKey(headers: secondHeaders);

        string? firstResult = await context.HttpCache.SendRequestWithCache(firstKey, null, HttpRequest);
        string? secondResult = await context.HttpCache.SendRequestWithCache(secondKey, null, HttpRequest);

        invocationCount.ShouldBe(2);
        firstResult.ShouldBe("payload-1");
        secondResult.ShouldBe("payload-2");
        context.HttpCache.GetCount().ShouldBe(2);
    }

    private sealed class HttpCacheManagerTestContext : IDisposable
    {
        internal HttpCacheManager HttpCache { get; }

        internal HttpCacheManagerTestContext(HttpCacheOptions? options = null)
        {
            HttpCache = new HttpCacheManager(NullLogger<HttpCacheManager>.Instance, options ?? new HttpCacheOptions());
        }

        internal HttpCacheKey CreateKey(string clientName = "client", string requestUrl = "https://service/resource", object? body = null, IReadOnlyDictionary<string, string>? headers = null)
        {
            return HttpCache.GetHttpCacheKey(clientName, requestUrl, body, headers);
        }

        public void Dispose()
        {
            HttpCache.Dispose();
        }
    }

    private sealed record ComplexPayload(string Id, NestedPayload Nested, IReadOnlyList<int> Scores);
    private sealed record NestedPayload(string Name, DateTime CreatedAt);
}
