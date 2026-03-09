using Microsoft.Extensions.DependencyInjection;
using Umea.se.Toolkit.Cache;
using Umea.se.Toolkit.Cache.Models;
using Umea.se.Toolkit.ClockInterface;
using Umea.se.Toolkit.Controllers;

namespace Umea.se.Toolkit.EntryPoints;

public static class CacheEntryPoint
{
    /// <summary>
    /// Sets up injection of a runtime/singleton Cache accessible by injecting Cache&lt;TKey, TData&gt;.
    /// Once the cache is full, any new addition will replace the oldest entry.
    /// </summary>
    /// <typeparam name="TKey">Type of the key to access data with</typeparam>
    /// <typeparam name="TData">Type of the data to be cached</typeparam>
    /// <param name="services">builder.Services</param>
    /// <param name="size">size of the cache</param>
    /// <param name="cacheValidity">Time to live for a cache entry. After this, it becomes invalid.</param>
    public static IServiceCollection AddCache<TKey, TData>(this IServiceCollection services, int size, TimeSpan cacheValidity)
        where TKey : notnull
        where TData : class
    {
        return services.AddSingleton<Cache<TKey, TData>>(s =>
            new Cache<TKey, TData>(s.GetRequiredService<IClock>(), size, cacheValidity));
    }

    /// <summary>
    /// Adds <see cref="HttpCacheManager"/> as Singleton and <see cref="CacheController"/> to controllers.
    /// This method has to be called to be able to use <see cref="ExternalService.ExternalServiceBaseWithCache"/>.
    /// </summary>
    public static IServiceCollection AddHttpCache(this IServiceCollection services)
    {
        services
            .AddSingleton<HttpCacheOptions>()
            .AddSingleton<HttpCacheManager>()
            .AddControllers()
            .ConfigureApplicationPartManager(partManager =>
            {
                partManager.FeatureProviders.Add(new ExplicitControllersFeatureProvider(typeof(CacheController)));
            });

        return services;
    }
}
