using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

public class ContextProviderCache : IContextProviderCache
{
    private readonly ContextProviderCacheOptions _options;
    private readonly IMemoryCache? _localCache;
    private readonly IDistributedCache? _distributedCache;

    public ContextProviderCache(
        ContextProviderCacheOptions options, 
        Func<ContextProviderCacheOptions, IMemoryCache?> memoryCache, 
        Func<ContextProviderCacheOptions, IDistributedCache?> distributedCache)
    {
        _options = options;
        _localCache = options.UseLocalCache ?  memoryCache(options) : null;
        _distributedCache = options.UseDistributedCache ? distributedCache(options) : null;
    }
    
    internal Func<DateTimeOffset> GetCurrentTime { get; set; } = () => DateTimeOffset.UtcNow;
    
    public async Task<ContextResult> GetOrCreateAsync(ContextKey contextKey, Func<Task<ContextResult>> createContextAsync, CancellationToken cancellationToken)
    {
        CachedContextResult cachedContextResult = await MemCacheGetOrCreateAsync( // first use local cache
            contextKey,
            () => DistributedCacheGetOrCreateAsync( // then look in distributed cache
                contextKey,
                CreateCacheItemAsync, // finally get the context from source
                cancellationToken
            )
        );

        return cachedContextResult.GetContextResult();
        
        async Task<CachedContextResult> CreateCacheItemAsync()
        {
            ContextResult contextResult = await createContextAsync();
            DateTimeOffset createdAt = GetCurrentTime();
            return new CachedContextResult(contextResult, createdAt);
        }
        
    }

    private async Task<CachedContextResult> DistributedCacheGetOrCreateAsync(ContextKey contextKey, 
        Func<Task<CachedContextResult>> createCacheItemAsync, CancellationToken cancellationToken)
    {
        if (!_options.UseDistributedCache || _distributedCache == null)
        {
            return await createCacheItemAsync();
        }

        CachedContextResult? cached;
        string? cachedStr;

        if (
            null != (cachedStr = await _distributedCache.GetStringAsync(contextKey.Key, cancellationToken))
            && null != (cached = JsonConvert.DeserializeObject<CachedContextResult>(cachedStr))
        )
        {
            // short circuit: the cache item was found
            return cached;
        }

        CachedContextResult newCached = await createCacheItemAsync();
        var newCachedStr = JsonConvert.SerializeObject(newCached);

        CacheInstruction instruction = newCached.GetContextResult().CacheInstruction;
        TimeSpan expiration = instruction.GetDistributedExpirationAtAge(GetCurrentTime() - newCached.CreationTime);
        if (expiration > TimeSpan.Zero)
        {
            await _distributedCache.SetStringAsync(contextKey.Key, newCachedStr, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                SlidingExpiration = _options.DistributedSlidingExpirationSeconds.HasValue
                    ? TimeSpan.FromSeconds(_options.DistributedSlidingExpirationSeconds.Value)
                    : null
            }, cancellationToken);
        }

        return newCached;
    }

    private async Task<CachedContextResult> MemCacheGetOrCreateAsync(
        ContextKey contextKey, Func<Task<CachedContextResult>> createCacheItemAsync)
    {
        if (!_options.UseLocalCache || _localCache == null)
        {
            return await createCacheItemAsync();
        }
        
        if (_localCache.TryGetValue(contextKey, out CachedContextResult? cacheItem) && cacheItem != null)
        {
            // short circuit: the cache item was found
            return cacheItem;
        }

        CachedContextResult newCachedContextResult = await createCacheItemAsync();
        CacheInstruction instruction = newCachedContextResult.GetContextResult().CacheInstruction;
        var expiration = instruction.GetLocalExpirationAtAge(GetCurrentTime() - newCachedContextResult.CreationTime);
        if (expiration > TimeSpan.Zero)
        {
            _localCache.Set(contextKey, newCachedContextResult, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration == TimeSpan.MaxValue ? null : expiration,
                SlidingExpiration = _options.LocalSlidingExpirationSeconds.HasValue
                    ? TimeSpan.FromSeconds(_options.LocalSlidingExpirationSeconds.Value)
                    : null,
                Priority = CacheItemPriority.Normal,
                
                // only calculate size (which is somewhat expensive) if local size limit was set
                Size = _options.LocalSizeLimit.HasValue
                    ? JsonConvert.SerializeObject(newCachedContextResult).Length
                    : null
            });
        }

        return newCachedContextResult;
    }

    
}