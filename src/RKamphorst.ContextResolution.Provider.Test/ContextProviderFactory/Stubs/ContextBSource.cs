using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

public class ContextBSource : IContextSource<Parameter, ContextB>
{
    public virtual async Task FillContextAsync(ContextB contextToFill, Parameter parameter, string? key,
        IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        if (parameter.IsContextANeededForContextB)
        {
            contextToFill.A = await contextProvider.GetContextAsync<ContextA>(cancellationToken: cancellationToken);
        }
    }
}