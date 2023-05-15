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

    /// <summary>
    /// Get the context sources for type <typeparamref name="TContext"/>
    /// </summary>
    /// <typeparam name="TContext">Tye type to get context sources for</typeparam>
    /// <returns>Enumerable of context sources, empty if none were found</returns>
    public IEnumerable<ITypedContextSource<TContext>> GetTypedContextSources<TContext>()
        where TContext : class =>
        (IEnumerable<ITypedContextSource<TContext>>)(_serviceProvider.GetService(
            typeof(IEnumerable<ITypedContextSource<TContext>>)
        ) ?? Enumerable.Empty<ITypedContextSource<TContext>>());

    /// <summary>
    /// Get all the named context sources 
    /// </summary>
    public IEnumerable<INamedContextSource> GetNamedContextSources() => _namedContextSources;
}