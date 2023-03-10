using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

public class ContextASource : IContextSource<Parameter, ContextA>
{
    public virtual async Task FillContextAsync(Parameter parameter, string? key,
        ContextA result,
        IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        if (parameter.IsContextBNeededForContextA)
        {
            result.B = await contextProvider.GetContextAsync<ContextB>(cancellationToken: cancellationToken);
        }
    }
}