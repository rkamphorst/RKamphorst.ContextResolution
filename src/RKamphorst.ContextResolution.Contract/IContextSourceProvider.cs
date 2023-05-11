namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Provides context sources of a given type
/// </summary>
public interface IContextSourceProvider
{
    /// <summary>
    /// Gets all context sources provide context of type <typeparamref name="TContext"/>
    /// </summary>
    /// <typeparam name="TContext">The type of context the context sources provide</typeparam>
    /// <seealso cref="ITypedContextSource{TContext}"/>
    /// <returns>
    ///   All typed context sources for <typeparamref name="TContext"/>,
    ///   empty enumerable if none were found
    /// </returns>
    public IEnumerable<ITypedContextSource<TContext>> GetTypedContextSources<TContext>()
        where TContext : class;

    /// <summary>
    /// Get all named context sources
    /// </summary>
    /// <seealso cref="INamedContextSource"/>
    /// <returns>All named context sources</returns>
    public IEnumerable<INamedContextSource> GetNamedContextSources();
}