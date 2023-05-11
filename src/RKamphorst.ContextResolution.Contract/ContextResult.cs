using Newtonsoft.Json.Linq;

namespace RKamphorst.ContextResolution.Contract;

public class ContextResult
{
    /// <summary>
    /// Create result indicating there were no context sources for the given name
    /// </summary>
    /// <remarks>
    /// Note the implication: it is the <see cref="ContextName"/> that did not result in any sources being found.
    /// A context source should never return this result, even if it deems the context not found; other mechanisms
    /// should be used for that.
    /// </remarks>
    /// <param name="name">The context name for which there were no sources.</param>
    /// <returns></returns>
    public static ContextResult NotFound(string name) 
        => new((ContextName) name, Enumerable.Empty<object>(), false, CacheInstruction.Transient);

    /// <summary>
    /// Create result indicating a successful (typed) result
    /// </summary>
    /// <param name="result">The result to report success for</param>
    /// <param name="cacheInstruction">Instruction how this result should be cached</param>
    /// <typeparam name="TContext">Type of the result; the <see cref="ContextName"/> is created from this</typeparam>
    /// <returns>The success context result</returns>
    public static ContextResult Success<TContext>(TContext result, CacheInstruction cacheInstruction)
        where TContext: class, new() =>
        new((ContextName)typeof(TContext), new object[] { result }, true, cacheInstruction);

    /// <summary>
    /// Create result indicating a successful (named) result
    /// </summary>
    /// <param name="name">Context name; the <see cref="ContextName"/> is created from this</param>
    /// <param name="result">The result to report success for</param>
    /// <param name="cacheInstruction">Instruction how this result should be cached</param>
    /// <returns>The success context result</returns>
    public static ContextResult Success(string name, object result, CacheInstruction cacheInstruction)
    {
        var contextName = (ContextName)name;
        return new ContextResult(contextName, new [] { result }, true, cacheInstruction);
    }

    /// <summary>
    /// Combine multiple context results into one
    /// </summary>
    /// <remarks>
    /// With multiple context sources, sometimes multiple results are available for the same context request.
    /// This method allows to merge those results into one.
    ///
    /// First, all the <see cref="NotFound"/> results are removed from the list. For the remaining items, the
    /// results' properties are combined into one object and coerced with <see cref="ContextName.Coerce"/>.
    /// If there are no remaining items, <see cref="NotFound"/> is returned.
    /// </remarks>
    /// <param name="contextName">context name all the results should have</param>
    /// <param name="results">Results to combine into one</param>
    /// <returns>The combined context result</returns>
    /// <exception cref="ArgumentException">If one of the context results has a different context name</exception>
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
    private object? _result;
    
    private ContextResult(ContextName name, IEnumerable<object> partialResults, bool isContextSourceFound, CacheInstruction cacheInstruction)
    {
        Name = name;
        _partialResults = partialResults;
        IsContextSourceFound = isContextSourceFound;
        CacheInstruction = cacheInstruction;
    }

    /// <summary>
    /// The context name this result is for
    /// </summary>
    public ContextName Name { get; }

    /// <summary>
    /// Get the result object
    /// </summary>
    /// <returns></returns>
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
    
    /// <summary>
    /// Whether the context source was found
    /// </summary>
    /// <remarks>
    /// <see cref="NotFound"/> sets this to false, <see cref="Success"/> sets this to true.
    /// </remarks>
    public bool IsContextSourceFound { get; }
    
    /// <summary>
    /// Instruction how to cache this context result.
    /// </summary>
    public CacheInstruction CacheInstruction { get; }

    
}

