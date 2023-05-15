using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

public interface IContextProviderCache
{
    /// <summary>
    /// Get a context from cache or create it (and add it to cache)
    /// </summary>
    /// <param name="contextKey">Key of the context to get or create</param>
    /// <param name="createContextAsync">Factory method for the creation if the item does not exist in cache</param>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns>The context result that was either gotten from cache or created just now</returns>
    Task<ContextResult> GetOrCreateAsync(ContextKey contextKey, Func<Task<ContextResult>> createContextAsync,
        CancellationToken cancellationToken);
}