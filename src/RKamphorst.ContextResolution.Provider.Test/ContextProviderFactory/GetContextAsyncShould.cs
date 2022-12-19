using System;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;
using Xunit;

// ReSharper disable MemberCanBePrivate.Global (because xUnit needs them to be public)
// ReSharper disable ClassNeverInstantiated.Global (because they are instantiated in mocks)

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory;

public class GetContextAsyncShould
{
    private readonly Mock<ContextASource> _mockContextASource;
    private readonly Mock<ContextBSource> _mockContextBSource;
    private readonly Mock<ContextCSource> _mockContextCSource;
    private readonly Mock<ContextDaSource> _mockContextDaSource;
    private readonly Mock<ContextDbSource> _mockContextDbSource;
    private readonly Mock<JsonObjectSource> _mockJsonObjectSource;

    private readonly Mock<IContextSourceProvider> _mockContextSourceProvider;
    private readonly Provider.ContextProviderFactory _sut;

    #region Verification and setup method expressions
    #pragma warning disable CS4014

    private static readonly Expression<Func<ContextASource, Task>> ContextASourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextA>(), It.IsAny<Parameter>(), It.IsAny<string>(),
            It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    private static readonly Expression<Func<ContextBSource, Task>> ContextBSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextB>(), It.IsAny<Parameter>(), It.IsAny<string>(),
            It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    private static readonly Expression<Action<ContextCSource>> ContextCSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextC>(), It.IsAny<Parameter>(), It.IsAny<string>(),
            It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    private static readonly Expression<Action<ContextDaSource>> ContextDaSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextD>(), It.IsAny<Parameter>(), It.IsAny<string>(),
            It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());
    
    private static readonly Expression<Action<ContextDbSource>> ContextDbSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<ContextD>(), It.IsAny<Parameter>(), It.IsAny<string>(),
            It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());

    private static readonly Expression<Action<JsonObjectSource>> JsonObjectSourceGetContextAsync = m =>
        m.FillContextAsync(It.IsAny<JsonObject>(), It.IsAny<Parameter>(), It.IsAny<string>(),
            It.IsAny<IContextProvider>(), It.IsAny<CancellationToken>());


    #pragma warning restore CS4014
    #endregion

    public GetContextAsyncShould()
    {
        _mockContextASource = new Mock<ContextASource> { CallBase = true };
        _mockContextBSource = new Mock<ContextBSource> { CallBase = true };
        _mockContextCSource = new Mock<ContextCSource> { CallBase = true };
        _mockJsonObjectSource = new Mock<JsonObjectSource> { CallBase = true };
        _mockContextDaSource = new Mock<ContextDaSource> { CallBase = true };
        _mockContextDbSource = new Mock<ContextDbSource> { CallBase = true };

        _mockContextSourceProvider = new Mock<IContextSourceProvider>();
        _mockContextSourceProvider
            .Setup(m => m.GetContextSources<Parameter, ContextA>())
            .Returns(new[] { _mockContextASource.Object });

        _mockContextSourceProvider
            .Setup(m => m.GetContextSources<Parameter, ContextB>())
            .Returns(new[] { _mockContextBSource.Object });

        _mockContextSourceProvider
            .Setup(m => m.GetContextSources<Parameter, ContextC>())
            .Returns(new[] { _mockContextCSource.Object });

        _mockContextSourceProvider
            .Setup(m => m.GetContextSources<Parameter, ContextD>())
            .Returns(new IContextSource<Parameter, ContextD>[]
                { _mockContextDaSource.Object, _mockContextDbSource.Object });

        _mockContextSourceProvider
            .Setup(m => m.GetContextSources<Parameter, JsonObject>())
            .Returns(new[] { _mockJsonObjectSource.Object });


        _sut = new Provider.ContextProviderFactory(_mockContextSourceProvider.Object);
    }

    [Fact]
    public async Task NotCallSourcesIfNoContextNeeded()
    {
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = false,
            IsContextBNeeded = false,
            IsContextBNeededForContextA = false,
            IsContextANeededForContextB = false
        });


        var contextC = await provider.GetContextAsync<ContextC>();

        contextC.A.Should().BeNull();
        contextC.B.Should().BeNull();
        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Never);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Never);
    }

    [Fact]
    public async Task ProvideContextFromNeededSource()
    {
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = false,
            IsContextBNeeded = true,
            IsContextBNeededForContextA = false,
            IsContextANeededForContextB = false
        });


        var contextC = await provider.GetContextAsync<ContextC>();

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
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = true,
            IsContextBNeeded = false,
            IsContextBNeededForContextA = true,
            IsContextANeededForContextB = false
        });

        var contextC = await provider.GetContextAsync<ContextC>();

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
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = false,
            IsContextBNeeded = true,
            IsContextBNeededForContextA = false,
            IsContextANeededForContextB = true
        });

        var contextC = await provider.GetContextAsync<ContextC>();

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
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = true,
            IsContextBNeeded = true,
            IsContextBNeededForContextA = false,
            IsContextANeededForContextB = true
        });

        var contextC = await provider.GetContextAsync<ContextC>();

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
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = true,
            IsContextBNeeded = false,
            IsContextBNeededForContextA = true,
            IsContextANeededForContextB = false
        });

        var contextD = await provider.GetContextAsync<ContextD>();

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
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = false,
            IsContextBNeeded = true,
            IsContextBNeededForContextA = true,
            IsContextANeededForContextB = true
        });

        Func<Task> act = async () => await provider.GetContextAsync<ContextC>();

        await act.Should().ThrowAsync<ContextCircularDependencyException>();

        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Once);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Once);
    }

    [Fact]
    public async Task ThrowIfContextSourceNotFound()
    {
        _mockContextSourceProvider
            .Setup(m => m.GetContextSources<Parameter, ContextB>())
            .Returns(Array.Empty<IContextSource<Parameter, ContextB>>());

        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = false,
            IsContextBNeeded = true,
            IsContextBNeededForContextA = false,
            IsContextANeededForContextB = false
        });

        Func<Task> act = async () => await provider.GetContextAsync<ContextC>();

        await act.Should().ThrowAsync<ContextSourceNotFoundException>();

        _mockContextCSource.Verify(ContextCSourceGetContextAsync, Times.Once);
        _mockContextASource.Verify(ContextASourceGetContextAsync, Times.Never);
        _mockContextBSource.Verify(ContextBSourceGetContextAsync, Times.Never);

    }
    
    [Fact]
    public async Task CreateContextWithFactory()
    {
        var parameter = new Parameter
        {
            IsContextANeeded = false,
            IsContextBNeeded = true,
            IsContextBNeededForContextA = false,
            IsContextANeededForContextB = false
        };
        IContextProvider provider = _sut.CreateContextProvider(parameter);
        
        JsonObject jsonObject = await provider.GetContextAsync(() => new JsonObject());

        jsonObject.TryGetPropertyValue("parameter", out var jsonParameter).Should().BeTrue();
        var ctxParameter = jsonParameter.Deserialize<Parameter>();
        ctxParameter.Should().BeEquivalentTo(parameter);
        
        _mockJsonObjectSource.Verify(JsonObjectSourceGetContextAsync, Times.Once);
    }

    [Theory]
    [InlineData("blabla")]
    [InlineData("ContextC")]
    [InlineData("RKamphorst.RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs.ContextC")]
    public async Task LookupContextByName(string contextCName)
    {
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = true,
            IsContextBNeeded = false,
            IsContextBNeededForContextA = true,
            IsContextANeededForContextB = false
        });

        var contextCObject = await provider.GetContextAsync(contextCName);

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

    
    [Fact]
    public async Task ThrowIfContextNameNotFound()
    {
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = true,
            IsContextBNeeded = false,
            IsContextBNeededForContextA = true,
            IsContextANeededForContextB = false
        });

        Func<Task> act = async () => await provider.GetContextAsync("no-such-name");

        await act.Should().ThrowAsync<ContextNameNotFoundException>();
    }
    
    [Fact]
    public async Task ThrowIfContextNameAmbiguous()
    {
        var provider = _sut.CreateContextProvider(new Parameter
        {
            IsContextANeeded = true,
            IsContextBNeeded = false,
            IsContextBNeededForContextA = true,
            IsContextANeededForContextB = false
        });

        Func<Task> act = async () => await provider.GetContextAsync("ambiguous");

        await act.Should().ThrowAsync<ContextNameAmbiguousException>();
    }
}