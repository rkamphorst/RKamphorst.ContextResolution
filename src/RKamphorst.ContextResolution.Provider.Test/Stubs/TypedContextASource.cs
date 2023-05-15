using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.Stubs;

public class TypedContextASource : ITypedContextSource<ContextA>
{
    public bool IsContextBNeeded { get; set; } = false;
    
    public virtual async Task<CacheInstruction> FillContextAsync(ContextA request, IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        if (IsContextBNeeded)
        {
            request.B = await contextProvider.GetContextAsync(new ContextB {Id = request.Id}, cancellationToken: cancellationToken);
        }
        return CacheInstruction.Transient;
    }
}