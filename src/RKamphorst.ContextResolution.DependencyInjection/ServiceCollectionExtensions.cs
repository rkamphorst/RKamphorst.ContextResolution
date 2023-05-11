using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.Provider;

namespace RKamphorst.ContextResolution.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add context provider service
    /// </summary>
    /// <param name="services">Service collection to add services to</param>
    /// <returns>The same services collection, to support chaining</returns>
    public static IServiceCollection AddContextProvider(this IServiceCollection services)
    {
        services
            .AddScoped<IContextProvider, ContextProvider>()
            .AddScoped<IContextSourceProvider, ContextSourceProvider>();
        
        return services;
    }

    /// <summary>
    /// Add caching capability for the context provider
    /// </summary>
    /// <param name="services">Service collection to add services to</param>
    /// <param name="configure">Configuration callback for the cache.</param>
    /// <returns>The same services collection for chainability</returns>
    public static IServiceCollection AddContextProviderCache(
        this IServiceCollection services, Action<ContextProviderCacheOptions> configure
        )
    {
        services.AddOptions();
        services.AddLogging();
        services.Configure(configure);
        services.AddSingleton<IContextProviderCache>(sp => new ContextProviderCache(
            sp.GetRequiredService<IOptions<ContextProviderCacheOptions>>().Value,
            opts =>
                opts.UseLocalCache
                    ? new MemoryCache(
                        new MemoryCacheOptions { SizeLimit = opts.LocalSizeLimit },
                        sp.GetRequiredService<ILoggerFactory>()
                    )
                    : null,
            opts =>
                opts.UseDistributedCache
                    ? sp.GetService<IDistributedCache>()
                    : null
        ));
        
        return services;
    }
}