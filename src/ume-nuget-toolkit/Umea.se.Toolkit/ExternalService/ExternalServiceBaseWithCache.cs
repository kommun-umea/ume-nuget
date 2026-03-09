using Umea.se.Toolkit.Cache;
using Umea.se.Toolkit.Cache.Models;
using Umea.se.Toolkit.EntryPoints;

namespace Umea.se.Toolkit.ExternalService;

/// <summary>
/// Extends <see cref="ExternalServiceBase"/> to add caching capabilities for HTTP requests.
/// Method <see cref="CacheEntryPoint.AddHttpCache"/> has to be called during application startup to enable caching.
/// </summary>
public abstract class ExternalServiceBaseWithCache : ExternalServiceBase
{
    private readonly HttpCacheManager _cacheManager;

    protected ExternalServiceBaseWithCache(
        string httpClientName,
        IHttpClientFactory httpClientFactory,
        HttpCacheManager cacheManager) : base(httpClientName, httpClientFactory)
    {
        _cacheManager = cacheManager;
    }

    protected async Task<T> HttpGetWithCache<T>(
        string requestUrl,
        IReadOnlyDictionary<string, string>? headers = null,
        HttpCacheEntryLifetime? entryLifetime = null)
    {
        return await SendRequestWithCache(
            requestUrl: requestUrl,
            requestLambda: async () => await HttpGet<T>(requestUrl, headers),
            requestBody: null,
            headers: headers,
            entryLifetime: entryLifetime);
    }

    protected async Task<T> HttpPostWithCache<T>(
        string requestUrl,
        object? requestBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        HttpCacheEntryLifetime? entryLifetime = null)
    {
        return await SendRequestWithCache(
            requestUrl: requestUrl,
            requestLambda: async () => await HttpPost<T>(requestUrl, requestBody, headers),
            requestBody: requestBody,
            headers: headers,
            entryLifetime: entryLifetime);
    }

    protected async Task<T?> HttpPostNullableWithCache<T>(
        string requestUrl,
        object? requestBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        HttpCacheEntryLifetime? entryLifetime = null)
    {
        return await SendNullableRequestWithCache(
            requestUrl: requestUrl,
            requestLambda: async () => await HttpPostNullable<T>(requestUrl, requestBody, headers),
            requestBody: requestBody,
            headers: headers,
            entryLifetime: entryLifetime);
    }

    protected async Task<T> HttpPutWithCache<T>(
        string requestUrl,
        object? requestBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        HttpCacheEntryLifetime? entryLifetime = null)
    {
        return await SendRequestWithCache(
            requestUrl: requestUrl,
            requestLambda: async () => await HttpPut<T>(requestUrl, requestBody, headers),
            requestBody: requestBody,
            headers: headers,
            entryLifetime: entryLifetime);
    }

    private async Task<T> SendRequestWithCache<T>(
        string requestUrl,
        Func<Task<T?>> requestLambda,
        object? requestBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        HttpCacheEntryLifetime? entryLifetime = null)
    {
        T? responseValue = await SendNullableRequestWithCache(requestUrl, requestLambda, requestBody, headers, entryLifetime);

        ArgumentNullException.ThrowIfNull(responseValue);

        return responseValue;
    }

    private async Task<T?> SendNullableRequestWithCache<T>(
        string requestUrl,
        Func<Task<T?>> requestLambda,
        object? requestBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        HttpCacheEntryLifetime? entryLifetime = null)
    {
        HttpCacheKey key = _cacheManager.GetHttpCacheKey(HttpClientName, requestUrl, requestBody, headers);
        T? responseValue = await _cacheManager.SendRequestWithCache(key, entryLifetime, requestLambda);

        return responseValue;
    }
}
