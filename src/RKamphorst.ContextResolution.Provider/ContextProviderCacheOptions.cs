namespace RKamphorst.ContextResolution.Provider;

public class ContextProviderCacheOptions
{
    /// <summary>
    /// Whether context provider should use a local (in memory) cache
    /// </summary>
    /// <remarks>
    /// Note that it is possible and even recommended to use this in combination with distributed cache!
    /// </remarks>
    public bool UseLocalCache { get; set; } = true;
    
    /// <summary>
    /// (Rough) size limit of the local cache
    /// </summary>
    /// <remarks>
    /// Setting this incurs a slight performance overhead, because at every cache insertion the size of the item has
    /// to be calculated. This is done by serializing it to JSON, which does not reflect the actual size in memory, but
    /// gives an upper bound.
    /// </remarks>
    public long? LocalSizeLimit { get; set; } = (1 << 30) ^ 2; // 2 GiB
    
    /// <summary>
    /// If a local cache item was not accessed for this period of time, it can be evicted
    /// </summary>
    public int? LocalSlidingExpirationSeconds { get; set; } = 60;

    /// <summary>
    /// Whether context provider should use a distributed cache (e.g. redis, memcached, ...)
    /// </summary>
    /// <remarks>
    /// Note that it is possible and even recommended to use this in combination with local cache!
    /// </remarks>
    public bool UseDistributedCache { get; set; } = false;
    
    /// <summary>
    /// If a distributed cache item was not accessed for this period of time, it can be evicted
    /// </summary>
    public int? DistributedSlidingExpirationSeconds { get; set; } = 900;
}