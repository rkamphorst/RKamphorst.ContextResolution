using Microsoft.Extensions.DependencyInjection;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.Provider;

namespace RKamphorst.ContextResolution.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContextResolution(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddScoped<IContextProviderFactory, ContextProviderFactory>()
            .AddScoped<IContextSourceProvider, ContextSourceProvider>();
        
        return serviceCollection;
    }
}