using Microsoft.Extensions.Logging;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

public class ContextProvider : IContextProvider
{
    /**
     * Machinery for invoking context sources.
     *
     * This was implemented separately because supporting a mix of typed
     * and named context sources proved to be quite complex.
     */
    private readonly IContextSourceInvoker _contextSourceInvoker;

    /**
     * Caching fields
     *
     * Two caching mechanisms:
     *  - "External" _cache, which caches across ContextProvider instances
     *  - "Internal" _invokeTasks, which caches invoke tasks during a context resolution,
     *     making sure no context source is invoked twice for the same context.
     *     NOTE: It is not cleared between GetContextAsync invocations!
     */
    private readonly IContextProviderCache? _cache;

    private readonly ILogger<ContextProvider> _logger;
    private readonly IDictionary<ContextKey, Task<ContextResult>> _invokeTasks;

    /**
     * Fields for child providers.
     *
     * Child providers are supplied to context sources so they can request
     * additional context. They perform circular dependency checks so that if
     * one occurs, the context sources will fail fast instead of ending up
     * in an endless loop.
     */
    private readonly ContextProvider? _parent;
    private readonly ContextKey? _forContext;

    public ContextProvider(IContextSourceProvider contextSourceProvider, ILogger<ContextProvider> logger) 
        : this(contextSourceProvider, null, logger) {}

    public ContextProvider(IContextSourceProvider contextSourceProvider, IContextProviderCache? cache, ILogger<ContextProvider> logger)
        : this(new AggregateContextSourceInvoker(new IContextSourceInvoker[]
        {
            new NamedContextSourceInvoker(contextSourceProvider, logger),
            new TypedContextSourceInvoker(contextSourceProvider, logger)
        }), cache, logger) { }

    internal ContextProvider(IContextSourceInvoker contextSourceInvoker, IContextProviderCache? cache, ILogger<ContextProvider> logger)
    {
        _contextSourceInvoker = contextSourceInvoker;
        
        _cache = cache;
        _logger = logger;
        _invokeTasks = new Dictionary<ContextKey, Task<ContextResult>>();
        
        _parent = null;
        _forContext = null;
    }
    
    private ContextProvider(ContextProvider parent, ContextKey forContext)
    {
        _contextSourceInvoker = parent._contextSourceInvoker;
        _cache = parent._cache;
        _invokeTasks = parent._invokeTasks;
        _logger = parent._logger;
        _parent = parent;
        _forContext = forContext;
        
    }

    public async Task<TContext> GetContextAsync<TContext>(TContext? requestedContext,
        bool requireAtLeastOneSource = false,
        CancellationToken cancellationToken = default) where TContext : class, new()
        => (TContext)(await GetContextAsync(ContextKey.FromTypedContext(requestedContext), requireAtLeastOneSource,
            cancellationToken)).GetResult();

    public async Task<object> GetContextAsync(string contextName, object? requestedContext,
        bool requireAtLeastOneSource = false,
        CancellationToken cancellationToken = default)
        => (await GetContextAsync(ContextKey.FromNamedContext(contextName, requestedContext),
            requireAtLeastOneSource,
            cancellationToken)).GetResult();
    
    private async Task<ContextResult> GetContextAsync(ContextKey key,
        bool requireAtLeastOneSource = false,
        CancellationToken cancellationToken = default)
    {
        using IDisposable? _ = _logger.BeginScope(new Dictionary<string, object>()
        {
            ["ContextKey"] = key,
            ["RequireAtLeastOneSource"] = requireAtLeastOneSource
        });
        
        _logger.LogDebug("Getting or creating context {ContextKey}", key);
        AssertNoCircularDependency(key);
        
        ContextResult contextResult = _cache != null
            ? await _cache.GetOrCreateAsync(key, InvokeSourcesAsync, cancellationToken)
            : await InvokeSourcesAsync();

        _logger.LogInformation("Got context result {@ContextResult}", contextResult);
        
        if (requireAtLeastOneSource && !contextResult.IsContextSourceFound)
        {
            throw new ContextSourceNotFoundException(contextResult.Name);
        }

        return contextResult;

        Task<ContextResult> InvokeSourcesAsync() => GetOrCreateInvokeTask(key, async () =>
        {
            _logger.LogInformation("Invoking context sources");
            ContextResult[] contextResults =
                await _contextSourceInvoker.GetContextResultsAsync(
                    key, new ContextProvider(this, key), cancellationToken
                );
            _logger.LogInformation("Invocation resulted in {ContextResultCount} context results to combine", key);
            return ContextResult.Combine(key.Name, contextResults);
        });
    }

    private Task<ContextResult> GetOrCreateInvokeTask(ContextKey key, Func<Task<ContextResult>> createAsync)
    {
        if (!_invokeTasks.TryGetValue(key, out Task<ContextResult>? result))
        {
            _logger.LogDebug("Reusing result from prior invocation");
            result = _invokeTasks[key] = createAsync();
        }

        return result;
    }
    
    private void AssertNoCircularDependency(ContextKey requestedContextKey)
    {
        ContextKey[] contextPath = EnumerateParentContextKeys().ToArray();
        if (contextPath.Contains(requestedContextKey))
        {
            throw new ContextCircularDependencyException(
                contextPath, 
                requestedContextKey
                );
        }
        
        IEnumerable<ContextKey> EnumerateParentContextKeys()
        {
            ContextProvider? parent = _parent;
            while (parent?._forContext != null)
            {
                yield return parent._forContext.Value;
                parent = parent._parent;
            }
        }
    }
    
}

