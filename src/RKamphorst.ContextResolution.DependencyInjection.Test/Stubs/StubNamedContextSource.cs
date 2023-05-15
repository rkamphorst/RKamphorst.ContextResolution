using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.DependencyInjection.Test.Stubs;

public class StubNamedContextSource : INamedContextSource
{
    public virtual Task<ContextResult[]> GetContextAsync(ContextKey key, IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        if (key.Name.Matches((ContextName)"StubContext"))
        {
            return
                Task.FromResult(
                    new[]
                    {
                        ContextResult.Success(
                            "StubContext", new { propertyfromnamedsource = "named" },
                            (CacheInstruction)"1m")
                    });
        }

        return Task.FromResult(Array.Empty<ContextResult>());
    }
}