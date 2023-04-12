using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

internal class TypedContextSourceInvoker : IContextSourceInvoker
{
    private readonly IContextSourceProvider _contextSourceProvider;

    public TypedContextSourceInvoker(IContextSourceProvider contextSourceProvider)
    {
        _contextSourceProvider = contextSourceProvider;
    }

    public Task<ContextResult[]> GetContextResultsAsync(
        ContextKey key, IContextProvider contextProvider, CancellationToken cancellationToken = default)
    {
        return GetContextResultsByKeyAsync(key, contextProvider, cancellationToken);
    }

    private async Task<ContextResult[]> GetTypedContextResultsAsync<TContext>(TContext id,
        IContextProvider contextProvider,
        CancellationToken cancellationToken = default) where TContext : class, new()
    {

        ITypedContextSource<TContext>[] contextSources =
            _contextSourceProvider.GetTypedContextSources<TContext>().ToArray();

        if (contextSources.Length == 0)
        {
            return Array.Empty<ContextResult>();
        }

        CacheInstruction cacheInstruction =
            CacheInstruction.Combine(await Task.WhenAll(contextSources.Select(FillFromSourceAsync)));

        return new[] { ContextResult.Success(id, cacheInstruction) };

        async Task<CacheInstruction> FillFromSourceAsync(ITypedContextSource<TContext> typedContextSource)
        {
            try
            {
                return await typedContextSource.FillContextAsync(
                    id, contextProvider, cancellationToken);
            }
            catch (ContextResolutionException)
            {
                // Some context failed to resolve from within the context source.
                // We raise it
                throw;
            }
            catch (Exception ex)
            {
                throw new ContextSourceFailedException(typedContextSource, ex);
            }
        }
    }

    public Task<ContextResult[]> GetContextResultsByKeyAsync(
        ContextKey key, IContextProvider contextProvider,
        CancellationToken cancellationToken = default)
    {
        IInvokerAdapter? adapter = GetOrCreateInvokerAdapter(key.Name);
        return adapter switch
        {
            null => Task.FromResult(new[] { ContextResult.NotFound((string) key.Name) }),
            _ => adapter.GetContextAsync(
                this, key, contextProvider, cancellationToken
            )
        };
    }

    private static IInvokerAdapter? GetOrCreateInvokerAdapter(ContextName contextName)
    {
        Type? typ = contextName.GetContextType();

        if (typ is null)
        {
            return null;
        }

        if (!ContextProviderInvokers.TryGetValue(typ, out IInvokerAdapter? invoker))
        {
            invoker =
                (IInvokerAdapter)Activator.CreateInstance(
                    typeof(InvokerAdapter<>).MakeGenericType(typ)
                )!;
            ContextProviderInvokers[typ] = invoker;
        }

        return invoker;
    }

    private static readonly Dictionary<Type, IInvokerAdapter> ContextProviderInvokers = new();

    private interface IInvokerAdapter
    {
        Task<ContextResult[]> GetContextAsync(TypedContextSourceInvoker contextSourceInvoker, ContextKey key,
            IContextProvider contextProvider,
            CancellationToken cancellationToken = default);
    }

    private class InvokerAdapter<TContext> : IInvokerAdapter
        where TContext : class, new()
    {
        public async Task<ContextResult[]> GetContextAsync(TypedContextSourceInvoker contextSourceInvoker,
            ContextKey key,
            IContextProvider contextProvider,
            CancellationToken cancellationToken = default)
        {
            return await contextSourceInvoker.GetTypedContextResultsAsync(
                (TContext)key.Id,
                contextProvider,
                cancellationToken
            );
        }
    }

}