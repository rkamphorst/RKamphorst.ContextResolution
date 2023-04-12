using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

internal interface IContextSourceInvoker
{
    public Task<ContextResult[]> GetContextResultsAsync(ContextKey contextKey,
        IContextProvider contextProvider, CancellationToken cancellationToken = default
    );
}