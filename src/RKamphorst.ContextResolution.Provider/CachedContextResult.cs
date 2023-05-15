using Newtonsoft.Json;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

/// <summary>
/// Represents a cached context result. Used by <see cref="ContextProviderCache"/>.
/// </summary>
public class CachedContextResult 
{
    private readonly ContextResult _contextResult;

    public CachedContextResult(ContextResult contextResult, DateTimeOffset creationTime)
        : this((string) contextResult.Name, contextResult.GetResult(), (string) contextResult.CacheInstruction, creationTime)
    {
        _contextResult = contextResult;
    }

    [JsonConstructor]
    public CachedContextResult(string contextName, object result, string cacheInstruction, DateTimeOffset creationTime)
    {
        _contextResult = ContextResult.Success(contextName, result, (CacheInstruction)cacheInstruction);
        CreationTime = creationTime;
    }

    [JsonProperty("n")]
    public string ContextName => (string) _contextResult.Name;

    [JsonProperty("i")]
    public string CacheInstruction => (string)_contextResult.CacheInstruction;

    [JsonProperty("r")]
    public object Result => _contextResult.GetResult();
    
    [JsonProperty("t")]
    public DateTimeOffset CreationTime { get; }

    public ContextResult GetContextResult() => _contextResult;
}