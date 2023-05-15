using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.DependencyInjection.Test.Stubs;

public class StubTypedContextSource : ITypedContextSource<StubContext>
{
    public Task<CacheInstruction> FillContextAsync(StubContext request, IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        request.PropertyFromTypedSource = "typed";
        return Task.FromResult((CacheInstruction)"1m");
    }
}