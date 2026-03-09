using System.Collections.Concurrent;
using Shouldly;
using Umea.se.Toolkit.Cache.Models;

namespace Umea.se.Toolkit.Test.Cache;

public partial class HttpCacheManagerTests
{
    // Ensures concurrent callers share the same in-flight result instead of triggering duplicates.
    [Fact]
    public async Task SendRequestWithCache_MultipleConcurrentRequests_SingleLambdaExecution()
    {
        using HttpCacheManagerTestContext context = new();

        HttpCacheKey cacheKey = context.CreateKey();
        string expectedPayload = "payload";
        int invocationCount = 0;

        async Task<string?> HttpRequest()
        {
            int callNumber = Interlocked.Increment(ref invocationCount);
            if (callNumber > 1)
            {
                throw new InvalidOperationException($"Lambda executed more than once. Call #{callNumber}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
            return expectedPayload;
        }

        Task<string?>[] requests = [.. Enumerable
            .Range(0, 20)
            .Select(_ => context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest))];

        string?[] results = await Task.WhenAll(requests);

        invocationCount.ShouldBe(1);
        results.ShouldAllBe(r => r == expectedPayload);
    }

    // Demonstrates distinct cache keys isolate execution even when requests overlap in time.
    [Fact]
    public async Task SendRequestWithCache_ConcurrentDistinctKeys_ExecutesEachLambdaOnce()
    {
        using HttpCacheManagerTestContext context = new();

        int totalInvocations = 0;
        ConcurrentDictionary<string, int> invocationPerKey = new();

        IEnumerable<Task<string?>> requests = Enumerable.Range(0, 10).Select(async index =>
        {
            HttpCacheKey cacheKey = context.CreateKey(
                clientName: $"client-{index}",
                requestUrl: $"https://service/resource/{index}",
                body: new { Id = index });

            async Task<string?> HttpRequest()
            {
                string result = $"payload-{index}";
                invocationPerKey.AddOrUpdate(result, 1, (_, existing) => existing + 1);
                Interlocked.Increment(ref totalInvocations);
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                return result;
            }

            return await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);
        });

        string?[] results = await Task.WhenAll(requests);

        totalInvocations.ShouldBe(10);
        invocationPerKey.Values.ShouldAllBe(count => count == 1);
        results.ShouldBe(Enumerable.Range(0, 10).Select(i => $"payload-{i}"));
    }

    // Verifies only one refresh occurs when the cache expires while many requests arrive simultaneously.
    [Fact]
    public async Task SendRequestWithCache_ConcurrentRequestsDuringCacheExpiration_OnlyOneRefresh()
    {
        using HttpCacheManagerTestContext context = new(new HttpCacheOptions
        {
            DefaultCacheEntryLifetime = new()
            {
                Maximum = TimeSpan.FromMilliseconds(100),
                Sliding = TimeSpan.FromMilliseconds(50)
            },
            MaxJitterOffsetPercentage = 0
        });

        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        async Task<string?> HttpRequest()
        {
            Interlocked.Increment(ref invocationCount);
            await Task.Delay(50);
            return $"payload-{invocationCount}";
        }

        await context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest);
        invocationCount.ShouldBe(1);

        await Task.Delay(1_100);

        Task<string?>[] requests = [.. Enumerable
            .Range(0, 20)
            .Select(_ => context.HttpCache.SendRequestWithCache(cacheKey, null, HttpRequest))];

        string?[] results = await Task.WhenAll(requests);

        invocationCount.ShouldBe(2);
        results.ShouldAllBe(r => r == "payload-2");
    }

    // Asserts that a shared failure is propagated to each waiter without rerunning the delegate.
    [Fact]
    public async Task SendRequestWithCache_ConcurrentRequestsWithFailure_AllRequestsFail()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        async Task<string?> FailingRequest()
        {
            Interlocked.Increment(ref invocationCount);
            await Task.Delay(50);
            throw new HttpRequestException("Simulated failure");
        }

        Task<string?>[] requests = [.. Enumerable
            .Range(0, 10)
            .Select(_ => context.HttpCache.SendRequestWithCache(cacheKey, null, FailingRequest))];

        Exception[] exceptions = await Task.WhenAll(
            requests.Select(async request => await Should.ThrowAsync<Exception>(() => request)));

        exceptions.ShouldAllBe(ex => ex is HttpRequestException);
        invocationCount.ShouldBe(1, "Failures should be shared across waiters without rerunning the delegate.");
        context.HttpCache.GetCount().ShouldBe(0);
    }

    // Confirms that failing request paths still release locks for subsequent successful calls.
    [Fact]
    public async Task SendRequestWithCache_ExceptionDuringConcurrentAccess_LocksAreCleanedUp()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        async Task<string?> FailingRequest()
        {
            Interlocked.Increment(ref invocationCount);
            await Task.Delay(100);
            throw new HttpRequestException("Simulated failure");
        }

        Task<string?>[] failingRequests = [.. Enumerable
            .Range(0, 50)
            .Select(_ => context.HttpCache.SendRequestWithCache(cacheKey, null, FailingRequest))];

        Exception[] exceptions = await Task.WhenAll(
            failingRequests.Select(async request => await Should.ThrowAsync<Exception>(() => request)));

        exceptions.ShouldAllBe(ex => ex is HttpRequestException);
        invocationCount.ShouldBe(1, "Failing lambda should execute once and release locks for later calls.");

        await Task.Delay(100);

        string? result = await context.HttpCache.SendRequestWithCache(
            cacheKey,
            null,
            () => Task.FromResult<string?>("success"));

        result.ShouldBe("success");
        context.HttpCache.GetCount().ShouldBe(1);
    }

    // Ensures independent keys remain isolated while sharing in-flight results per key.
    [Fact]
    public async Task SendRequestWithCache_ConcurrentMixOfSuccessAndFailure_EachKeyIsolated()
    {
        using HttpCacheManagerTestContext context = new();

        int successInvocations = 0;
        int failureInvocations = 0;

        HttpCacheKey successKey = context.CreateKey(requestUrl: "https://service/success");
        HttpCacheKey failureKey = context.CreateKey(requestUrl: "https://service/failure");

        async Task<string?> SuccessRequest()
        {
            Interlocked.Increment(ref successInvocations);
            await Task.Delay(50);
            return "success-data";
        }

        async Task<string?> FailingRequest()
        {
            Interlocked.Increment(ref failureInvocations);
            await Task.Delay(50);
            throw new HttpRequestException("Failure");
        }

        Task<string?>[] successTasks = Enumerable.Range(0, 20)
            .Select(_ => context.HttpCache.SendRequestWithCache(successKey, null, SuccessRequest))
            .ToArray();

        Task<string?>[] failureTasks = Enumerable.Range(0, 20)
            .Select(_ => context.HttpCache.SendRequestWithCache(failureKey, null, FailingRequest))
            .ToArray();

        string?[] successResults = await Task.WhenAll(successTasks);
        successResults.ShouldAllBe(r => r == "success-data");
        successInvocations.ShouldBe(1, "Success lambda should only execute once");

        Exception[] failureExceptions = await Task.WhenAll(
            failureTasks.Select(async task => await Should.ThrowAsync<Exception>(() => task)));

        failureExceptions.ShouldAllBe(ex => ex is HttpRequestException);
        failureInvocations.ShouldBe(1, "Failure lambda should execute only once and share the exception.");

        context.HttpCache.GetCount().ShouldBe(1);
    }

    // Demonstrates that only one retry executes after an initial shared failure.
    [Fact]
    public async Task SendRequestWithCache_FailureThenConcurrentRetries_OnlyOneRetryExecutes()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;
        bool shouldFail = true;

        async Task<string?> ConditionalRequest()
        {
            Interlocked.Increment(ref invocationCount);
            await Task.Delay(50);

            if (shouldFail)
            {
                throw new HttpRequestException("Simulated failure");
            }
            return "success";
        }

        await Should.ThrowAsync<HttpRequestException>(
            () => context.HttpCache.SendRequestWithCache(cacheKey, null, ConditionalRequest));

        invocationCount.ShouldBe(1);
        context.HttpCache.GetCount().ShouldBe(0);

        shouldFail = false;

        Task<string?>[] retryRequests = [.. Enumerable
            .Range(0, 20)
            .Select(_ => context.HttpCache.SendRequestWithCache(cacheKey, null, ConditionalRequest))];

        string?[] results = await Task.WhenAll(retryRequests);

        results.ShouldAllBe(r => r == "success");
        invocationCount.ShouldBe(2, "Only one retry should execute the lambda");
        context.HttpCache.GetCount().ShouldBe(1);
    }

    // Validates that a failure is shared across concurrent callers without re-executing the lambda.
    [Fact]
    public async Task SendRequestWithCache_ConcurrentFailures_ShareSameException()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();
        int invocationCount = 0;

        async Task<string?> VaryingExceptionRequest()
        {
            int callNumber = Interlocked.Increment(ref invocationCount);
            await Task.Delay(30);

            if (callNumber == 1)
            {
                throw new HttpRequestException("First failure");
            }
            else if (callNumber == 2)
            {
                throw new TimeoutException("Second failure");
            }
            else
            {
                throw new InvalidOperationException("Third failure");
            }
        }

        async Task VerifyBatchAsync()
        {
            const int batchSize = 9;

            Task<string?>[] tasks = [.. Enumerable
                .Range(0, batchSize)
                .Select(_ => context.HttpCache.SendRequestWithCache(cacheKey, null, VaryingExceptionRequest))];

            Exception[] exceptions = await Task.WhenAll(
                tasks.Select(async task => await Should.ThrowAsync<Exception>(() => task)));

            exceptions.Length.ShouldBe(batchSize);
            exceptions.ShouldAllBe(ex => ex is HttpRequestException && ex.Message == "First failure");
        }

        await VerifyBatchAsync();
        invocationCount.ShouldBe(1);

        invocationCount = 0;

        await VerifyBatchAsync();
        invocationCount.ShouldBe(1);

        context.HttpCache.GetCount().ShouldBe(0);
    }

    // Proves locks are freed after a failure so a later attempt can succeed.
    [Fact]
    public async Task SendRequestWithCache_LambdaThrowsException_LockIsProperlyReleased()
    {
        using HttpCacheManagerTestContext context = new();
        HttpCacheKey cacheKey = context.CreateKey();

        static Task<string?> httpRequest() =>
            throw new HttpRequestException("Simulated failure");

        await Should.ThrowAsync<HttpRequestException>(
            () => context.HttpCache.SendRequestWithCache(cacheKey, null, httpRequest));

        string? result = await context.HttpCache.SendRequestWithCache(
            cacheKey,
            null,
            () => Task.FromResult<string?>("success"));

        result.ShouldBe("success");
    }
}
