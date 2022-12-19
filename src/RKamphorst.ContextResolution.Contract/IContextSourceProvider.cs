namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Provides context sources of a given type
/// </summary>
public interface IContextSourceProvider
{
    /// <summary>
    /// Gets all context sources with parameter type <typeparamref name="TParameter"/> that update
    /// context of type <typeparamref name="TContext"/>
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter the context sources get context for</typeparam>
    /// <typeparam name="TContext">The type of context the context sources provide</typeparam>
    /// <returns></returns>
    public IEnumerable<IContextSource<TParameter, TContext>> GetContextSources<TParameter, TContext>()
        where TContext : class;
}