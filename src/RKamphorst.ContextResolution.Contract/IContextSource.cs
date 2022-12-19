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
    /// Multiple context sources might receive the same instance of <paramref name="contextToFill"/>.
    /// Implementers must make sure they do only nondestructive updates to <paramref name="contextToFill"/>,
    /// so only add to lists and dictionaries, and do not replace non-default values if present.
    /// </remarks>
    /// <param name="contextToFill">The context to update</param>
    /// <param name="parameter">The parameter to update the context for</param>
    /// <param name="key">Extra key to update context for (optional)</param>
    /// <param name="contextProvider">Context provider to fetch additional required context</param>
    Task FillContextAsync(TContext contextToFill, TParameter parameter, string? key,
        IContextProvider contextProvider, CancellationToken cancellationToken);
}