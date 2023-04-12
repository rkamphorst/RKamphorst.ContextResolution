using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.DependencyInjection;

public class ContextSourceProvider : IContextSourceProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<INamedContextSource> _namedContextSources;

    public ContextSourceProvider(IServiceProvider serviceProvider, IEnumerable<INamedContextSource> namedContextSources)
    {
        _serviceProvider = serviceProvider;
        _namedContextSources = namedContextSources;
    }

    public IEnumerable<ITypedContextSource<TContext>> GetTypedContextSources<TContext>()
        where TContext : class =>
        (IEnumerable<ITypedContextSource<TContext>>)(_serviceProvider.GetService(
            typeof(IEnumerable<ITypedContextSource<TContext>>)
        ) ?? Enumerable.Empty<ITypedContextSource<TContext>>());

    public IEnumerable<INamedContextSource> GetNamedContextSources() => _namedContextSources;
}