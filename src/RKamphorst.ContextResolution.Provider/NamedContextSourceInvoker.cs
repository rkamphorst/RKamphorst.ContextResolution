using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

internal class NamedContextSourceInvoker : IContextSourceInvoker
{
    private readonly IContextSourceProvider _contextSourceProvider;

    public NamedContextSourceInvoker(IContextSourceProvider contextSourceProvider)
    {
        _contextSourceProvider = contextSourceProvider;
    }

    public async Task<ContextResult[]> GetContextResultsAsync(ContextKey key, IContextProvider contextProvider,
        CancellationToken cancellationToken = default)
    {
        INamedContextSource[] contextSources = _contextSourceProvider.GetNamedContextSources().ToArray();

        return (await Task.WhenAll(contextSources.Select(GetFromSourceAsync)))
            .SelectMany(s => s).ToArray();
        
            
        async Task<ContextResult[]> GetFromSourceAsync(INamedContextSource namedContextSource)
        {
            try
            {
                return await namedContextSource.GetContextAsync(
                    key, contextProvider, cancellationToken);
            }
            catch (ContextResolutionException)
            {
                // Some context failed to resolve from within the context source.
                // We re-throw it
                throw;
            }
            catch (Exception ex)
            {
                throw new ContextSourceFailedException(namedContextSource, ex);
            }
        }
    }
}