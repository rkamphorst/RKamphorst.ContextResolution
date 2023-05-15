using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RKamphorst.ContextResolution.Contract;
using Xunit;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderCache;
using ContextProviderCache = Provider.ContextProviderCache;

public class ConstructorShould
{
    [Fact]
    public async Task AllowMemoryCacheFactoryToReturnNullEventIfUseLocalCacheIsTrue()
    {
        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseLocalCache = true
            }, 
            _ => null,
            _ => Mock.Of<IDistributedCache>(),
            Mock.Of<ILogger<ContextProviderCache>>()
        );

        ContextResult result =
            await sut.GetOrCreateAsync(ContextKey.FromNamedContext("ctx"), 
                () => Task.FromResult(ContextResult.Success("ctx", new { }, CacheInstruction.Transient)),
                CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AllowDistributedCacheFactoryToReturnNullEventIfUseLocalCacheIsTrue()
    {
        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseDistributedCache = true
            },
            _ => Mock.Of<IMemoryCache>(),
            _ => null,
            Mock.Of<ILogger<ContextProviderCache>>()
        );

        ContextResult result =
            await sut.GetOrCreateAsync(ContextKey.FromNamedContext("ctx"), 
                () => Task.FromResult(ContextResult.Success("ctx", new { }, CacheInstruction.Transient)),
                CancellationToken.None);

        result.Should().NotBeNull();
    }
    
    
    
}