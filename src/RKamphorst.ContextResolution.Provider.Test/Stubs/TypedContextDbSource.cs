using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.Stubs;

public class TypedContextDbSource : ITypedContextSource<ContextD>
{
    public bool IsContextBNeeded { get; set; } = false;
    public virtual async Task<CacheInstruction> FillContextAsync(ContextD request, IContextProvider contextProvider,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        request.B = IsContextBNeeded
            ? await contextProvider.GetContextAsync(
                new ContextB { Id = request.Id },
                cancellationToken: cancellationToken)
            : null;
        return CacheInstruction.Transient;
    }
}