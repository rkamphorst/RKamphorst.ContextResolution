using Newtonsoft.Json.Linq;

namespace RKamphorst.ContextResolution.Contract;

public class ContextResult
{
    public static ContextResult NotFound(string name) 
        => new((ContextName) name, Enumerable.Empty<object>(), false, CacheInstruction.Transient);

    public static ContextResult Success<TContext>(TContext result, CacheInstruction cacheInstruction)
        where TContext: class, new() =>
        new((ContextName)typeof(TContext), new object[] { result }, true, cacheInstruction);

    public static ContextResult Success(string name, object result, CacheInstruction cacheInstruction)
    {
        var contextName = (ContextName)name;
        return new ContextResult(contextName, new [] { result }, true, cacheInstruction);
    }

    public static ContextResult Combine(
        ContextName contextName, IEnumerable<ContextResult> results)
    {
        ContextResult[] resultsArray = results.ToArray();

        if (resultsArray.Any(n => !contextName.Matches(n.Name)))
        {
            throw new ArgumentException($"One or more results are for context other than {contextName}",
                nameof(results));
        }

        if (resultsArray.Length == 1)
        {
            return resultsArray[0];
        }

        ContextResult[] foundResultsArray = resultsArray
            .Where(r => r.IsContextSourceFound).ToArray();

        return foundResultsArray switch
        {
            { Length: 0 } => NotFound(contextName.Key),
            { Length: 1 } => foundResultsArray[0],
            _ =>
                new ContextResult(
                    contextName,
                    foundResultsArray.SelectMany(r => r._partialResults),
                    true,
                    CacheInstruction.Combine(foundResultsArray.Select(r => r.CacheInstruction))
                )
        };
    }

    private readonly IEnumerable<object> _partialResults;
    private object? _result = null;
    
    private ContextResult(ContextName name, IEnumerable<object> partialResults, bool isContextSourceFound, CacheInstruction cacheInstruction)
    {
        Name = name;
        _partialResults = partialResults;
        IsContextSourceFound = isContextSourceFound;
        CacheInstruction = cacheInstruction;
    }

    public ContextName Name { get; }

    public object GetResult()
    {
        if (_result != null)
        {
            return _result;
        }

        var resultArray = _partialResults.ToArray();
        switch (resultArray.Length)
        {
            case 1:
                _result = Name.Coerce(resultArray[0]);
                break;
            default:
            {
                JObject mergedResult = _partialResults
                    .Select(r => r as JObject ?? JObject.FromObject(r))
                    .Aggregate(new JObject(), (a, b) =>
                    {
                        b.Merge(a, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Concat,
                            MergeNullValueHandling = MergeNullValueHandling.Ignore,
                            PropertyNameComparison = StringComparison.InvariantCultureIgnoreCase
                        });
                        return b;
                    });
                _result = Name.Coerce(mergedResult);
                break;
            }
        }

        return _result;
    }
    
    public bool IsContextSourceFound { get; }
    
    public CacheInstruction CacheInstruction { get; }

    
}

