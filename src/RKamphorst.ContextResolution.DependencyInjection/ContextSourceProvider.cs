using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.DependencyInjection;

public class ContextSourceProvider : IContextSourceProvider
{
    private readonly IServiceProvider _serviceProvider;

    public ContextSourceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<IContextSource<TParameter, TContext>> GetContextSources<TParameter, TContext>()
        where TContext : class =>
        (IEnumerable<IContextSource<TParameter, TContext>>)(_serviceProvider.GetService(
            typeof(IEnumerable<IContextSource<TParameter, TContext>>)
        ) ?? Enumerable.Empty<IContextSource<TParameter, TContext>>());
}