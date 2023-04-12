namespace RKamphorst.ContextResolution.Provider;

public class ContextProviderCacheOptions
{
    public bool UseLocalCache { get; set; } = true;
    public long? LocalSizeLimit { get; set; } = (1 << 30) ^ 2; // 2 GiB
    public int? LocalSlidingExpirationSeconds { get; set; } = 60;

    public bool UseDistributedCache { get; set; } = false;
    public int? DistributedSlidingExpirationSeconds { get; set; } = 900;
}