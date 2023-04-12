using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.Stubs;

public class TypedContextBSource : ITypedContextSource<ContextB>
{
    public bool IsContextANeeded { get; set; } = false;

    public virtual async Task<CacheInstruction> FillContextAsync(ContextB request, IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        if (IsContextANeeded)
        {
            request.A = await contextProvider.GetContextAsync(new ContextA { Id = request.Id },
                cancellationToken: cancellationToken);
        }

        return CacheInstruction.Transient;
    }
}