namespace Umea.se.Toolkit.Cache.Models;

internal readonly record struct HttpCacheKey(string HttpClientName, string RequestUrl, string BodyHash, string HeadersHash);
