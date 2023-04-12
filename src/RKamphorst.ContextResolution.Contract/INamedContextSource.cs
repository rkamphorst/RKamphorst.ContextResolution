namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Represents a source of context 
/// </summary>
/// <typeparam name="TContext">The type of context this is a source of</typeparam>
public interface INamedContextSource
{
    /// <summary>
    /// Returns a context, given a name and a context request
    /// </summary>
    /// <remarks>
    /// Where <see cref="ITypedContextSource{TContext}"/> returns context of a specific type,
    /// this context source potentially returns any type of context, given <paramref name="contextName"/>
    /// and <paramref name="request"/> (an "empty" context object with only an id).
    ///
    /// Because this context source may not be able to return certain contexts, it is also able to
    /// return the fact that it does not now a certain context name, see <see cref="ContextResult.NotFound"/>
    /// </remarks>
    /// <param name="key">The context key (context name + request ed context -- e.g. an ID)</param>
    /// <param name="contextProvider">Context provider to fetch additional required context</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Results indicating whether a context was successfully fetched and how long the result can be cached</returns>
    Task<ContextResult[]> GetContextAsync(
        ContextKey key, IContextProvider contextProvider, CancellationToken cancellationToken
    );
}