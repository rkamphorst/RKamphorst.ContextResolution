using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

public interface IContextProviderCache
{
    Task<ContextResult> GetOrCreateAsync(ContextKey contextKey, Func<Task<ContextResult>> createContextAsync,
        CancellationToken cancellationToken);
}