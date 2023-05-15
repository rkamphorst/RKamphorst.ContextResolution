using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.Provider.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderCache;
using ContextProviderCache = Provider.ContextProviderCache;

public class GetOrCreateAsyncShould
{
    [Theory]
    [InlineData(false, true, true, true, "transient", false, true, false, false)]
    [InlineData(false, true, true, true, "1m", false, true, false, true)]
    [InlineData(true, true, true, true, "transient", true, true, false, false)]
    [InlineData(true, true, true, true, "1m", true, true, true, true)]
    [InlineData(false, false, true, true, "transient", false, false, false, false)]
    [InlineData(false, false, true, true, "1m", false, false, false, false)]
    [InlineData(true, false, true, true, "transient", true, false, false, false)]
    [InlineData(true, false, true, true, "1m", true, false, true, false)]
    [InlineData(false, true, false, true, "transient", false, true, false, false)]
    [InlineData(false, true, false, true, "1m", false, true, false, true)]
    [InlineData(true, true, false, true, "transient", false, true, false, false)]
    [InlineData(true, true, false, true, "1m", false, true, false, true)]
    [InlineData(false, false, false, true, "transient", false, false, false, false)]
    [InlineData(false, false, false, true, "1m", false, false, false, false)]
    [InlineData(true, false, false, true, "transient", false, false, false, false)]
    [InlineData(true, false, false, true, "1m", false, false, false, false)]
    [InlineData(false, true, true, false, "transient", false, false, false, false)]
    [InlineData(false, true, true, false, "1m", false, false, false, false)]
    [InlineData(true, true, true, false, "transient", true, false, false, false)]
    [InlineData(true, true, true, false, "1m", true, false, true, false)]
    [InlineData(false, false, true, false, "transient", false, false, false, false)]
    [InlineData(false, false, true, false, "1m", false, false, false, false)]
    [InlineData(true, false, true, false, "transient", true, false, false, false)]
    [InlineData(true, false, true, false, "1m", true, false, true, false)]
    [InlineData(false, true, false, false, "transient", false, false, false, false)]
    [InlineData(false, true, false, false, "1m", false, false, false, false)]
    [InlineData(true, true, false, false, "transient", false, false, false, false)]
    [InlineData(true, true, false, false, "1m", false, false, false, false)]
    [InlineData(false, false, false, false, "transient", false, false, false, false)]
    [InlineData(false, false, false, false, "1m", false, false, false, false)]
    [InlineData(true, false, false, false, "transient", false, false, false, false)]
    [InlineData(true, false, false, false, "1m", false, false, false, false)]
    public async Task UseCache(
        bool useLocalCache, 
        bool useDistributedCache, 
        bool createLocalCache,
        bool createDistributedCache,
        string cacheInstruction, 
        bool shouldGetFromLocalCache,
        bool shouldGetFromDistributedCache,
        bool shouldAddToLocalCache,
        bool shouldAddToDistributedCache)
    {
        var mockLocalCache = new Mock<IMemoryCache>();
        mockLocalCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());
        var mockDistributedCache = new Mock<IDistributedCache>();

        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseLocalCache = useLocalCache,
                UseDistributedCache = useDistributedCache
            },
            _ => createLocalCache ? mockLocalCache.Object : null,
            _ => createDistributedCache ? mockDistributedCache.Object : null,
            Mock.Of<ILogger<ContextProviderCache>>()
        );

        var id = new ContextA { Id = "id1" };
        var ctx = new ContextA
        {
            Id = "id1",
            B = new ContextB
            {
                Id = "idB"
            }
        };

        await sut.GetOrCreateAsync(ContextKey.FromTypedContext(id),
            () => Task.FromResult(ContextResult.Success(ctx, (CacheInstruction) cacheInstruction)),
            CancellationToken.None);

        mockDistributedCache.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), shouldGetFromDistributedCache ? Times.Once : Times.Never);
        mockDistributedCache.Verify(
            m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), shouldAddToDistributedCache ? Times.Once : Times.Never);
        mockLocalCache.Verify(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny), shouldGetFromLocalCache ? Times.Once : Times.Never);
        mockLocalCache.Verify(m => m.CreateEntry(It.IsAny<object>()), shouldAddToLocalCache ? Times.Once : Times.Never);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(12345L, null)]
    [InlineData(null, 54321)]
    public async Task UseLocalCacheOptions(long? localSizeLimit, int? localSlidingExpirationSeconds)
    {
        ContextProviderCacheOptions? cacheCreationOptions = null;
        var mockLocalCache = new Mock<IMemoryCache>();
        var stubCacheEntry = new StubCacheEntry("key");
        
        mockLocalCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
            .Returns(false);
        mockLocalCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(stubCacheEntry);

        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseLocalCache = true,
                UseDistributedCache = false,
                LocalSizeLimit = localSizeLimit,
                LocalSlidingExpirationSeconds = localSlidingExpirationSeconds
            },
            options =>
            {
                cacheCreationOptions = options;
                return mockLocalCache.Object;
            },
            _ => null,
            Mock.Of<ILogger<ContextProviderCache>>()
        );

        var id = new ContextA { Id = "id1" };
        var ctx = new ContextA
        {
            Id = "id1",
            B = new ContextB
            {
                Id = "idB"
            }
        };

        await sut.GetOrCreateAsync(ContextKey.FromTypedContext(id),
            () => Task.FromResult(ContextResult.Success(ctx, (CacheInstruction)"1m")),
            CancellationToken.None);

        mockLocalCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);
        stubCacheEntry.SlidingExpiration.Should().Be(
            !localSlidingExpirationSeconds.HasValue
                ? null
                : TimeSpan.FromSeconds(localSlidingExpirationSeconds.Value)
        );
        
        if (localSizeLimit.HasValue)
        {
            stubCacheEntry.Size.Should().NotBeNull();
        }
        else
        {
            stubCacheEntry.Size.Should().BeNull();
        }
        
        cacheCreationOptions!.LocalSizeLimit.Should().Be(localSizeLimit);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(12345)]
    public async Task UseDistributedCacheOptions(int? distributedSlidingExpirationSeconds)
    {
        var mockDistributedCache = new Mock<IDistributedCache>();

        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseLocalCache = false,
                UseDistributedCache = true,
                DistributedSlidingExpirationSeconds = distributedSlidingExpirationSeconds
            },
            _ => null,
            _ => mockDistributedCache.Object,
            Mock.Of<ILogger<ContextProviderCache>>()
        );

        var id = new ContextA { Id = "id1" };
        var ctx = new ContextA
        {
            Id = "id1",
            B = new ContextB
            {
                Id = "idB"
            }
        };

        await sut.GetOrCreateAsync(ContextKey.FromTypedContext(id),
            () => Task.FromResult(ContextResult.Success(ctx, (CacheInstruction)"1m")),
            CancellationToken.None);

        var expectedDistributedSlidingExpiration = distributedSlidingExpirationSeconds.HasValue
            ? (TimeSpan?) TimeSpan.FromSeconds(distributedSlidingExpirationSeconds.Value)
            : null;
        
        if (expectedDistributedSlidingExpiration.HasValue)
        {
            mockDistributedCache.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                    It.Is<DistributedCacheEntryOptions>(o =>
                        o.SlidingExpiration == expectedDistributedSlidingExpiration), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        else
        {
            mockDistributedCache.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                    It.Is<DistributedCacheEntryOptions>(o =>
                        o.SlidingExpiration == null), It.IsAny<CancellationToken>()),
                Times.Once);

        }
    }

    [Fact]
    public async Task AddToDistributedCache()
    {
        var mockCache = new Mock<IDistributedCache>();

        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseDistributedCache = true
            },
            _ => null,
            _ => mockCache.Object,
            Mock.Of<ILogger<ContextProviderCache>>()
        );

        var id = new ContextA { Id = "id1" };
        var ctx = new ContextA
        {
            Id = "id1",
            B = new ContextB
            {
                Id = "idB"
            }
        };

        await sut.GetOrCreateAsync(ContextKey.FromTypedContext(id),
            () => Task.FromResult(ContextResult.Success(ctx, (CacheInstruction)"1m")),
            CancellationToken.None);

        mockCache.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCache.Verify(
            m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotInvokeSourcesIfPresentInDistributedCache()
    {
        var cacheDict = new Dictionary<string, byte[]>();
        var mockDistributedCache = new Mock<IDistributedCache>();
        mockDistributedCache
            .Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, byte[], DistributedCacheEntryOptions, CancellationToken>((key, value, _, _) =>
            {
                cacheDict[key] = value;
                return Task.CompletedTask;
            });
        mockDistributedCache
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((key, _)
                => Task.FromResult(cacheDict.TryGetValue(key, out var result) ? result : null));

        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseDistributedCache = true
            },
            _ => null,
            _ => mockDistributedCache.Object,
            Mock.Of<ILogger<ContextProviderCache>>()
        );

        var id = new ContextA { Id = "id1" };
        var ctx = new ContextA
        {
            Id = "id1",
            B = new ContextB
            {
                Id = "idB"
            }
        };

        var context1 = await sut.GetOrCreateAsync(ContextKey.FromTypedContext(id),
            () => Task.FromResult(ContextResult.Success(ctx, (CacheInstruction)"1m")),
            CancellationToken.None);
        var context2 = await sut.GetOrCreateAsync(ContextKey.FromTypedContext(id),
            () => throw new InvalidOperationException("The context result should come from cache the second time"),
            CancellationToken.None);
        
        context1.Should().BeEquivalentTo(context2);
        
        mockDistributedCache
            .Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockDistributedCache.Verify(
            m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task NotInvokeSourcesIfPresentInLocalCache()
    {
        var now = DateTimeOffset.Parse("2022-01-02T03:00Z");
        var id = new ContextA { Id = "id1" };
        var ctx = new ContextA
        {
            Id = "id1",
            B = new ContextB
            {
                Id = "idB"
            }
        };
        var expectContextResult = ContextResult.Success(ctx, (CacheInstruction)"1m");
        
        var mockLocalCache = new Mock<IMemoryCache>();
        mockLocalCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
            .Callback((object _, out object value) =>
            {
                value = new CachedContextResult(expectContextResult, now);
            })
            .Returns(true);

        var sut = new ContextProviderCache(
            new ContextProviderCacheOptions
            {
                UseLocalCache = true
            },
            _ => mockLocalCache.Object,
            _ => null,
            Mock.Of<ILogger<ContextProviderCache>>()
        )
        {
            GetCurrentTime = () => now
        };


        var contextResult = await sut.GetOrCreateAsync(ContextKey.FromTypedContext(id),
            () => throw new InvalidOperationException("The context result should come from cache"),
            CancellationToken.None);
        
        contextResult.Should().BeEquivalentTo(expectContextResult);
        
        mockLocalCache
            .Verify(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny), Times.Exactly(1));
    }
}