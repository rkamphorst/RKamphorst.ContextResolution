using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.DependencyInjection.Test.Stubs;
using RKamphorst.ContextResolution.Provider;
using Xunit;

namespace RKamphorst.ContextResolution.DependencyInjection.Test.ServiceCollectionExtensions;

public class AddContextProviderCacheShould
{
    [Fact]
    public void AddContextProviderCache()
    {
        var services = new ServiceCollection();

        services.AddContextProviderCache(_ => { });
        services.Should().Contain(s => s.ServiceType == typeof(IContextProviderCache));
    }

    [Fact]
    public async Task IntroduceLocalCaching()
    {
        var services = new ServiceCollection();
        var mockNamedContextSource = new Mock<StubNamedContextSource> { CallBase = true };

        var contextProvider = services
            .AddContextProvider()
            .AddTransient<INamedContextSource>(_ => mockNamedContextSource.Object)
            .AddContextProviderCache(o =>
            {
                o.UseLocalCache = true;
                o.UseDistributedCache = false;
            })
            .BuildServiceProvider()
            .GetService<IContextProvider>();

        var context1 = await contextProvider!.GetContextAsync("StubContext", null);
        var context2 = await contextProvider!.GetContextAsync("StubContext", null);

        context1.Should().BeEquivalentTo(context2);
        mockNamedContextSource.Verify(
            m => m.GetContextAsync(
                It.IsAny<ContextKey>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>()
            ), Times.Once
        );
    }

    [Fact]
    public async Task IntroduceDistributedCaching()
    {
        var services = new ServiceCollection();
        var mockNamedContextSource = new Mock<StubNamedContextSource> { CallBase = true };

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

        var contextProvider = services
            .AddContextProvider()
            .AddTransient<INamedContextSource>(_ => mockNamedContextSource.Object)
            .AddSingleton<IDistributedCache>(_ => mockDistributedCache.Object)
            .AddContextProviderCache(o =>
            {
                o.UseDistributedCache = true;
                o.UseLocalCache = false;
            })
            .BuildServiceProvider()
            .GetService<IContextProvider>();

        var context1 = await contextProvider!.GetContextAsync("StubContext", null);
        var context2 = await contextProvider!.GetContextAsync("StubContext", null);

        context1.Should().BeEquivalentTo(context2);
        mockNamedContextSource.Verify(
            m => m.GetContextAsync(
                It.IsAny<ContextKey>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>()
            ), Times.Once
        );
        mockDistributedCache
            .Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockDistributedCache.Verify(
            m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        
    }
}