using System;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

public class ContextDaSource : IContextSource<Parameter, ContextD>
{
    public virtual async Task FillContextAsync(Parameter parameter, string? key,
        ContextD result,
        IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        result.A = parameter.IsContextANeeded
            ? await contextProvider.GetContextAsync<ContextA>(cancellationToken: cancellationToken)
            : null;
    }
}