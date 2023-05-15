using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.Stubs;

public class TypedContextDaSource : ITypedContextSource<ContextD>
{
    public bool IsContextANeeded { get; set; } = false;
    
    public virtual async Task<CacheInstruction> FillContextAsync(ContextD request, IContextProvider contextProvider,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        request.A = IsContextANeeded
            ? await contextProvider.GetContextAsync(
                new ContextA { Id = request.Id },
                cancellationToken: cancellationToken)
            : null;
        return CacheInstruction.Transient;
    }
}