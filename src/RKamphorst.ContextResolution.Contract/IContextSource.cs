namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Represents a source context <typeparamref name="TContext"/>
/// </summary>
/// <typeparam name="TParameter"></typeparam>
/// <typeparam name="TContext"></typeparam>
public interface IContextSource<in TParameter, in TContext>
    where TContext : class
{
    /// <summary>
    /// Fills a given context
    /// </summary>
    /// <remarks>
    /// Multiple context sources might receive the same instance of <paramref name="resultContext"/>.
    /// Implementers must make sure they do only nondestructive updates to <paramref name="resultContext"/>,
    /// so only add to lists and dictionaries, and do not replace non-default values if present.
    /// </remarks>
    /// <param name="parameter">The parameter to update the context for</param>
    /// <param name="key">Extra key to update context for (optional)</param>
    /// <param name="result">The result (context) to update</param>
    /// <param name="contextProvider">Context provider to fetch additional required context</param>
    /// <param name="cancellationToken"></param>
    Task FillContextAsync(TParameter parameter, string? key,
        TContext result,
        IContextProvider contextProvider, CancellationToken cancellationToken);
}