using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

internal class AggregateContextSourceInvoker : IContextSourceInvoker
{
    private readonly IEnumerable<IContextSourceInvoker> _invokers;

    public AggregateContextSourceInvoker(IEnumerable<IContextSourceInvoker> invokers)
    {
        _invokers = invokers;
    }

    public async Task<ContextResult[]> GetContextResultsAsync(
        ContextKey key,
        IContextProvider contextProvider,
        CancellationToken cancellationToken = default)
    {
        return (await Task.WhenAll(
            _invokers.Select(
                i => i.GetContextResultsAsync(key, contextProvider, cancellationToken)
            )
        )).SelectMany(r => r).ToArray();
    }
}