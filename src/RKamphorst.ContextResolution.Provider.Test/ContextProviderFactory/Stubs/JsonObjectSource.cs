using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

public class JsonObjectSource : IContextSource<Parameter, JsonObject>
{
    public virtual async Task FillContextAsync(Parameter parameter, string? key,
        JsonObject result,
        IContextProvider contextProvider,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50)), cancellationToken);
        result["parameter"] = JsonSerializer.SerializeToNode(parameter);
    }
}