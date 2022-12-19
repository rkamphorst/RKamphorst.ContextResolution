using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

/// <summary>
/// Creates context providers for parameters.
/// </summary>
/// <typeparam name="TParameter">Type of the parameter to get context for</typeparam>
public class ContextProviderFactory : IContextProviderFactory
{
    private readonly IContextSourceProvider _contextSourceProvider;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contextSourceProvider">
    /// Provider of context sources that will be used in building context providers
    /// </param>
    public ContextProviderFactory(IContextSourceProvider contextSourceProvider)
    {
        _contextSourceProvider = contextSourceProvider;
    }

    /// <summary>
    /// Create a context provider
    /// </summary>
    /// <remarks>
    /// Note: A context provider that is returned from this method should be used in a limited scope, that is:
    /// if you want context to be queried from sources *again*, you need to create a new context provider.
    ///
    /// The context provider makes sure:
    /// * any context source is queried at most once in its lifetime: the very first response is cached
    /// * context sources are invoked in the correct order even if they in turn require other context
    ///  
    /// </remarks>
    /// <param name="parameter">Parameter to create the context provider for</param>
    /// <typeparam name="TParameter">Type of the parameter to create the context provider for</typeparam>
    public IContextProvider CreateContextProvider<TParameter>(TParameter parameter)
    {
        return new PrivateContextProvider<TParameter>(parameter, _contextSourceProvider);
    }
    
    private class PrivateContextProvider<TParameter> : IContextProvider
    {
        private readonly TParameter _parameter;
        private readonly IContextSourceProvider _contextSourceProvider;
        private readonly IDictionary<(Type, string?), Task<object>> _contexts;

        private readonly PrivateContextProvider<TParameter>? _parent;
        private readonly Type? _forContext;

        public PrivateContextProvider(TParameter parameter, IContextSourceProvider contextSourceProvider)
        {
            _parent = null;
            _forContext = null;
            _parameter = parameter;
            _contextSourceProvider = contextSourceProvider;
            _contexts = new Dictionary<(Type, string?), Task<object>>();
        }

        private PrivateContextProvider(PrivateContextProvider<TParameter> parent, Type forContext)
        {
            _parent = parent;
            _forContext = forContext;
            _parameter = _parent._parameter;
            _contextSourceProvider = _parent._contextSourceProvider;
            _contexts = _parent._contexts;
        }

        public Task<TContext> GetContextAsync<TContext>(string? key = null, CancellationToken cancellationToken = default)
            where TContext : class, new()
            => GetContextAsync(() => new TContext(), key, cancellationToken);

        public async Task<TContext> GetContextAsync<TContext>(Func<TContext> createNewContext, string? key = null,
            CancellationToken cancellationToken = default) where TContext : class
        {
            Type[] contextPath = EnumerateParentContexts().ToArray();
            Type contextType = typeof(TContext);
            if (contextPath.Contains(contextType))
            {
                throw new ContextCircularDependencyException(contextPath, typeof(TContext));
            }

            if (_contexts.TryGetValue((contextType, key), out Task<object>? contextTask))
            {
                return (TContext)await contextTask;
            }

            IContextSource<TParameter, TContext>[]? contextSources = 
                _contextSourceProvider.GetContextSources<TParameter, TContext>().ToArray();

            if (contextSources == null || contextSources.Length == 0)
            {
                throw new ContextSourceNotFoundException(contextType);
            }

            async Task<object> FillFromContextSourcesAsync()
            {
                TContext resultContext = createNewContext();
                await Task.WhenAll(
                    contextSources.Select(cs =>
                        FillContextFromSourceAsync(cs, _parameter, key, resultContext, cancellationToken))
                );
                return resultContext;
            }

            contextTask = FillFromContextSourcesAsync();
            _contexts[(contextType, key)] = contextTask;

            return (TContext)await contextTask;
        }

        private async Task FillContextFromSourceAsync<TContext>(IContextSource<TParameter, TContext> contextSource,
            TParameter parameter, string? key,
            TContext contextToFill,
            CancellationToken cancellationToken)
            where TContext : class
        {
            var contextProvider = new PrivateContextProvider<TParameter>(this, typeof(TContext));
            try
            {
                await contextSource.FillContextAsync(
                    contextToFill,
                    parameter,
                    key,
                    contextProvider,
                    cancellationToken
                );
            }
            catch (ContextResolutionException)
            {
                // Some context failed to resolve withing the context source.
                // We pass it through.
                throw;
            }
            catch (Exception ex)
            {
                throw new ContextSourceFailedException(contextSource.GetType().FullName, ex);
            }
        }

        public Task<object> GetContextAsync(string typeName, string? key = null, CancellationToken cancellationToken = default) => 
            ContextProviderTypeResolutionHelper.GetContextAsync(this, typeName, key, cancellationToken);

        private IEnumerable<Type> EnumerateParentContexts()
        {
            var parent = _parent;
            while (parent?._forContext != null)
            {
                yield return parent._forContext;
                parent = parent._parent;
            }
        }
    }
}