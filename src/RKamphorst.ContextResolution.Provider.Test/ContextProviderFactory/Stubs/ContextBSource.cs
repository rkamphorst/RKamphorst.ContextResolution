using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

public class ContextBSource : IContextSource<Parameter, ContextB>
{
    public virtual async Task FillContextAsync(Parameter parameter, string? key,
        ContextB result,
        IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        if (parameter.IsContextANeededForContextB)
        {
            result.A = await contextProvider.GetContextAsync<ContextA>(cancellationToken: cancellationToken);
        }
    }
}