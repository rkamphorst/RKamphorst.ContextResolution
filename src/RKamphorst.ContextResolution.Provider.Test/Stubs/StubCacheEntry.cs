using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace RKamphorst.ContextResolution.Provider.Test.Stubs;

public class StubCacheEntry : ICacheEntry
{
    public StubCacheEntry(object key)
    {
        Key = key;
    }
    
    public object Key { get; }
    public object? Value { get; set; }
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }

    public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

    public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

    public CacheItemPriority Priority { get; set; }
    public long? Size { get; set; }

    public void Dispose() { }
}