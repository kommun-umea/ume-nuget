using Microsoft.AspNetCore.Mvc;
using Umea.se.Toolkit.Auth;
using Umea.se.Toolkit.Cache;
using Umea.se.Toolkit.Controllers.Models;

namespace Umea.se.Toolkit.Controllers;

[Produces("application/json")]
[Route(ApiRoutesBase.Cache)]
internal class CacheController : ControllerBase
{
    private readonly HttpCacheManager _cacheManager;

    public CacheController(HttpCacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    [HttpGet("count")]
    public CacheEntryCountResponse GetCacheCount()
    {
        return new CacheEntryCountResponse
        {
            CacheEntries = _cacheManager.GetCount(),
        };
    }

    [HttpPost("clear")]
    [AuthorizeApiKey]
    public CacheClearResponse ClearCache()
    {
        return new CacheClearResponse
        {
            CacheEntriesCleared = _cacheManager.Clear(),
        };
    }
}
