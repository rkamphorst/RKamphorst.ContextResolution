using Microsoft.Extensions.Logging;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

internal class NamedContextSourceInvoker : IContextSourceInvoker
{
    private readonly IContextSourceProvider _contextSourceProvider;
    private readonly ILogger _logger;

    public NamedContextSourceInvoker(IContextSourceProvider contextSourceProvider, ILogger logger)
    {
        _contextSourceProvider = contextSourceProvider;
        _logger = logger;
    }

    public async Task<ContextResult[]> GetContextResultsAsync(ContextKey key, IContextProvider contextProvider,
        CancellationToken cancellationToken = default)
    {
        INamedContextSource[] contextSources = _contextSourceProvider.GetNamedContextSources().ToArray();

        _logger.LogDebug("Invoking {NamedContextSourceCount} named context sources", contextSources.Length);
        var result = (await Task.WhenAll(contextSources.Select(GetFromSourceAsync)))
            .SelectMany(s => s).ToArray();
        
        _logger.LogInformation(
            "Got {NamedContextResultCount} results from {NamedContextSourceCount} named context sources",
            result.Count(r => r.IsContextSourceFound),
            contextSources.Length
            );

        return result;
            
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