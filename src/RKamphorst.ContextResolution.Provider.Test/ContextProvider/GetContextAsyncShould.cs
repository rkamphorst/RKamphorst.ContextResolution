using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.Provider.Test.Stubs;
using Xunit;

// ReSharper disable MemberCanBePrivate.Global (because xUnit needs them to be public)
// ReSharper disable ClassNeverInstantiated.Global (because they are instantiated in mocks)

namespace RKamphorst.ContextResolution.Provider.Test.ContextProvider;

public class GetContextAsyncShould
{
    private readonly Mock<TypedContextASource> _mockContextASource;
    private readonly Mock<TypedContextBSource> _mockContextBSource;
    private readonly Mock<TypedContextCSource> _mockContextCSource;
    private readonly Mock<TypedContextDaSource> _mockContextDaSource;
    private readonly Mock<TypedContextDbSource> _mockContextDbSource;
    private readonly Mock<INamedContextSource> _mockNamedContextSource;

    private readonly Mock<IContextSourceProvider> _mockContextSourceProvider;
    private readonly Provider.ContextProvider _sut;

    #region Verification and setup method expressions
    #pragma warning disable CS4014

    private static readonly Expression<Func<TypedContextASource, Task>> ContextASourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextA>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    private static readonly Expression<Func<TypedContextBSource, Task>> ContextBSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextB>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    private static readonly Expression<Func<TypedContextCSource, Task>> ContextCSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextC>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    private static readonly Expression<Func<TypedContextDaSource, Task>> ContextDaSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextD>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());
    
    private static readonly Expression<Func<TypedContextDbSource, Task>> ContextDbSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextD>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    #pragma warning restore CS4014
    #endregion

    public GetContextAsyncShould()
    {
        _mockContextASource = new Mock<TypedContextASource> { CallBase = true };
        _mockContextBSource = new Mock<TypedContextBSource> { CallBase = true };
        _mockContextCSource = new Mock<TypedContextCSource> { CallBase = true };
        _mockContextDaSource = new Mock<TypedContextDaSource> { CallBase = true };
        _mockContextDbSource = new Mock<TypedContextDbSource> { CallBase = true };
        _mockNamedContextSource = new Mock<INamedContextSource>();

        _mockNamedContextSource.Setup(
                m => m.GetContextAsync(It.IsAny<ContextKey>(), It.IsAny<IContextProvider>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns((ContextKey key, IContextProvider provider, CancellationToken _) =>
            {
                if (key.Name.GetContextType() != null)
                {
                    return Task.FromResult(Array.Empty<ContextResult>());
                }
                
                return Task.FromResult(new[]
                {
                    ContextResult.Success((string)key.Name, new { name = (string)key.Name }, CacheInstruction.Transient)
                });
            });
            
        
        _mockContextSourceProvider = new Mock<IContextSourceProvider>();
        _mockContextSourceProvider
            .Setup(m => m.GetTypedContextSources<ContextA>())
            .Returns(new[] { _mockContextASource.Object });

        _mockContextSourceProvider
            .Setup(m => m.GetTypedContextSources<ContextB>())
            .Returns(new[] { _mockContextBSource.Object });

        _mockContextSourceProvider
            .Setup(m => m.GetTypedContextSources<ContextC>())
            .Returns(new[] { _mockContextCSource.Object });

        _mockContextSourceProvider
            .Setup(m => m.GetTypedContextSources<ContextD>())
            .Returns(new ITypedContextSource<ContextD>[]
                { _mockContextDaSource.Object, _mockContextDbSource.Object });

        _mockContextSourceProvider.Setup(
            m => m.GetNamedContextSources()
        ).Returns(new INamedContextSource[] { _mockNamedContextSource.Object });

        _sut = new Provider.ContextProvider(_mockContextSourceProvider.Object);
    }

    [Fact]
    public async Task NotCallSourcesIfNoContextNeeded()
    {

        var contextC = await _sut.GetContextAsync(new ContextC{ Id = "1" });

        contextC.A.Should().BeNull();
        contextC.B.Should().BeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Never);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Never);
    }

    [Fact]
    public async Task ProvideContextFromNeededSource()
    {
        _mockContextCSource.Object.IsContextBNeeded = true;
        var contextC = await _sut.GetContextAsync(new ContextC { Id = "id" });

        contextC.A.Should().BeNull();
        contextC.B.Should().NotBeNull();
        contextC.B!.A.Should().BeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Never);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }

    [Fact]
    public async Task ProvideContextDependenciesA()
    {
        _mockContextCSource.Object.IsContextANeeded = true;
        _mockContextASource.Object.IsContextBNeeded = true;
        var contextC = await _sut.GetContextAsync(new ContextC { Id = "id" });

        contextC.A.Should().NotBeNull();
        contextC.A!.B.Should().NotBeNull();
        contextC.B.Should().BeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Once);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }

    [Fact]
    public async Task ProvideContextDependenciesB()
    {
        _mockContextCSource.Object.IsContextBNeeded = true;
        _mockContextBSource.Object.IsContextANeeded = true;
        var contextC = await _sut.GetContextAsync(new ContextC { Id = "id" });

        contextC.A.Should().BeNull();
        contextC.B.Should().NotBeNull();
        contextC.B!.A.Should().NotBeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Once);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }

    [Fact]
    public async Task InvokeContextSourcesOnlyOnce()
    {
        _mockContextCSource.Object.IsContextANeeded = true;
        _mockContextCSource.Object.IsContextBNeeded = true;
        _mockContextBSource.Object.IsContextANeeded = true;
        var contextC = await _sut.GetContextAsync(new ContextC { Id = "id" });

        contextC.A.Should().NotBeNull();
        contextC.B.Should().NotBeNull();
        contextC.B!.A.Should().NotBeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Once);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }
    
    [Fact]
    public async Task SupportMultipleContextProvidersForOneContextType()
    {
        _mockContextDaSource.Object.IsContextANeeded = true;
        _mockContextASource.Object.IsContextBNeeded = true;
        var contextD = await _sut.GetContextAsync(new ContextD { Id = "id"});

        contextD.Should().NotBeNull();
        contextD.A.Should().NotBeNull();
        contextD.A!.B.Should().NotBeNull();
        contextD.B.Should().BeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Never);
        _mockContextDaSource.Verify(ContextDaSourceGetContextAsync, Times.Once);
        _mockContextDbSource.Verify(ContextDbSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Once);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }

    [Fact]
    public async Task ThrowIfCircularDependencyDetected()
    {
        _mockContextCSource.Object.IsContextANeeded = true;
        _mockContextCSource.Object.IsContextBNeeded = true;
        _mockContextASource.Object.IsContextBNeeded = true;
        _mockContextBSource.Object.IsContextANeeded = true;

        try
        {
            await _sut.GetContextAsync(new ContextC { Id = "id" });
            Assert.Fail($"Expected {nameof(ContextCircularDependencyException)} to be thrown");
        }
        catch (ContextCircularDependencyException ex)
        {
            ex.ContextKeyPath.Should().HaveCount(2);
            ex.ContextKeyPath[0].Should().Be((ContextKey)"{\"ContextA\":{\"id\":\"id\"}}");
            ex.ContextKeyPath[1].Should().Be((ContextKey)"{\"ContextC\":{\"id\":\"id\"}}");
            ex.RequestedContextKey.Should().Be((ContextKey)"{\"ContextA\":{\"id\":\"id\"}}");
        }

        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Once);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }

    [Fact]
    public async Task ThrowIfContextSourceNotFound()
    {
        _mockContextSourceProvider
            .Setup(m => m.GetTypedContextSources<ContextC>())
            .Returns(Array.Empty<ITypedContextSource<ContextC>>());

        Func<Task> act = async () =>
        {
            await _sut.GetContextAsync(new ContextC { Id = "id" }, requireAtLeastOneSource: true);
        };

        (await act.Should().ThrowAsync<ContextSourceNotFoundException>())
            .Where(e => e.ContextName == (ContextName)typeof(ContextC));

        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Never);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Never);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Never);
    }

    [Theory]
    [InlineData("context-c-alias")]
    [InlineData("other-context-c-alias")]
    [InlineData("ContextC")]
    public async Task LookupTypedContextByName(string contextCName)
    {
        _mockContextCSource.Object.IsContextANeeded = true;
        _mockContextASource.Object.IsContextBNeeded = true;
        var contextCObject = await _sut.GetContextAsync(contextCName, new { Id = "id" });

        var contextC = contextCObject as ContextC;
        contextCObject.Should().NotBeNull();
        contextC.Should().NotBeNull();
        contextC!.A.Should().NotBeNull();
        contextC.A!.B.Should().NotBeNull();
        contextC.B.Should().BeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Once);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }

    [Theory]
    [InlineData("named-context-a")]
    [InlineData("named-context-b")]
    public async Task InvokeNamedContextSources(string contextName)
    {
        var contextObject = await _sut.GetContextAsync(contextName, null);

        contextObject.Should().BeEquivalentTo(new { name = contextName });
        _mockNamedContextSource.Verify(m => m.GetContextAsync(
                It.IsAny<ContextKey>(),
                It.IsAny<IContextProvider>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task ThrowIfContextNameAmbiguous()
    {
        _mockContextCSource.Object.IsContextANeeded = true;
        _mockContextASource.Object.IsContextBNeeded = true;

        Func<Task> act = async () => await _sut.GetContextAsync("ambiguous", new { Id = "id" });

        await act.Should().ThrowAsync<ContextNameAmbiguousException>();
    }

    [Fact]
    public async Task ThrowIfTypedContextSourceThrows()
    {
        var mockContextSource = new Mock<ITypedContextSource<ContextA>>();
        mockContextSource.Setup(
            m => m.FillContextAsync(It.IsAny<ContextA>(), It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>())
        ).ThrowsAsync(new StubException());
        
        _mockContextSourceProvider
            .Setup(m => m.GetTypedContextSources<ContextA>())
            .Returns(new[] { mockContextSource.Object });

        Func<Task> act = async () => await _sut.GetContextAsync(new ContextA());

        try
        {
            await _sut.GetContextAsync(new ContextA());
            Assert.Fail($"Expecting {nameof(ContextSourceFailedException)} to be thrown");
        }
        catch (ContextSourceFailedException ex)
        {
            ex.InnerException.Should().BeOfType<StubException>();
            ex.ContextSource.Should().Be(mockContextSource.Object);
        }
        
        (await act.Should().ThrowAsync<ContextSourceFailedException>())
            .Where(e => e.InnerException is StubException)
            .Where(e => e.ContextSource == mockContextSource.Object);
    }
    
    [Fact]
    public async Task ThrowIfNamedContextSourceThrows()
    {
        _mockNamedContextSource.Setup(
            m => m.GetContextAsync(It.IsAny<ContextKey>(), It.IsAny<IContextProvider>(),
                It.IsAny<CancellationToken>()
            )
        ).Throws(new StubException());

        Func<Task> act = async () => await _sut.GetContextAsync("named-context", null);

        await act.Should().ThrowAsync<ContextSourceFailedException>()
            .Where(e => e.InnerException is StubException);
    }
    
    [Fact]
    public async Task ThrowIfNamedContextSourceHasCircularDependency()
    {
        _mockNamedContextSource.Setup(
                m => m.GetContextAsync(It.IsAny<ContextKey>(), It.IsAny<IContextProvider>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(async (ContextKey key, IContextProvider provider, CancellationToken c) =>
            {
                await provider.GetContextAsync("named-context", null, false, c);
                return Array.Empty<ContextResult>();
            });

        Func<Task> act = async () => await _sut.GetContextAsync("named-context", null);

        await act.Should().ThrowAsync<ContextCircularDependencyException>();
    }
}