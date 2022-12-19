using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

public class ContextCSource : IContextSource<Parameter, ContextC>
{
    public virtual async Task FillContextAsync(ContextC contextToFill, Parameter parameter, string? key,
        IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        contextToFill.A = parameter.IsContextANeeded
            ? await contextProvider.GetContextAsync<ContextA>(cancellationToken: cancellationToken)
            : null;
        contextToFill.B = parameter.IsContextBNeeded
            ? await contextProvider.GetContextAsync<ContextB>(cancellationToken: cancellationToken)
            : null;
    }
}