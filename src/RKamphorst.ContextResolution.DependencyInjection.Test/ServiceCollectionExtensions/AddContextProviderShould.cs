using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.DependencyInjection.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.DependencyInjection.Test.ServiceCollectionExtensions;

public class AddContextProviderShould
{

    [Fact]
    public void AddContextProvider()
    {
        var services = new ServiceCollection();
        
        services.AddContextProvider();
        services.Should().Contain(s => s.ServiceType == typeof(IContextProvider));
    }
    
    [Fact]
    public void AddInstantiableContextProvider()
    {
        var services = new ServiceCollection();

        var serviceProvider = services.AddContextProvider().BuildServiceProvider();
        var contextProvider = serviceProvider.GetService<IContextProvider>();

        contextProvider.Should().NotBeNull();
    }
    
    [Fact]
    public async Task AddContextProviderThatGetsContext()
    {
        var services = new ServiceCollection();
        var contextProvider = services.AddContextProvider().BuildServiceProvider().GetService<IContextProvider>();
        var context = await contextProvider!.GetContextAsync("context-by-name", null);
        context.Should().NotBeNull();
    }
    
    [Fact]
    public async Task AddContextProviderThatUsesNamedContextSources()
    {
        var services = new ServiceCollection();

        var serviceProvider = 
        services
            .AddContextProvider()
            .AddTransient<INamedContextSource, StubNamedContextSource>()
            .BuildServiceProvider();
        
        var contextProvider = serviceProvider.GetService<IContextProvider>();

        var context = await contextProvider!.GetContextAsync("StubContext", null);
        context.Should().BeOfType<StubContext>();
        context.Should().BeEquivalentTo(new StubContext { PropertyFromNamedSource = "named" });
    }
    
    [Fact]
    public async Task AddContextProviderThatUsesTypedContextSources()
    {
        var services = new ServiceCollection();
        
        var serviceProvider = 
            services
                .AddContextProvider()
                .AddTransient<ITypedContextSource<StubContext>, StubTypedContextSource>()
                .BuildServiceProvider();
        
        var contextProvider = serviceProvider.GetService<IContextProvider>();

        var context = await contextProvider!.GetContextAsync("StubContext", null);
        context.Should().BeOfType<StubContext>();
        context.Should().BeEquivalentTo(new StubContext { PropertyFromTypedSource = "typed" });
    }
    
    
}