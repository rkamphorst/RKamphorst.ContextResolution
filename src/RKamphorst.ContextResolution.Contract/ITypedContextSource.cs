namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Represents a source of strongly typed context <typeparamref name="TContext"/>
/// </summary>
/// <remarks>
/// A Typed Context Source provides one specific type of context (<paramref name="TContext"/>).
/// 
/// It does so through the <see cref="FillContextAsync"/> method, which does not "return" a new
/// context, but rather updates the context object that it is given as a request.
/// See <see cref="FillContextAsync"/> for more on the reason for this pattern.
/// </remarks>
/// <typeparam name="TContext">The type of context this is a source of</typeparam>
public interface ITypedContextSource<in TContext>
    where TContext : class
{
    /// <summary>
    /// Fills the given context
    /// </summary>
    /// <remarks>
    /// This method does not "return" a new context, but rather updates the given context
    /// object <see cref="request"/>. When passed to the method, it should have some properties
    /// set (such as an Id, or other information that indicates what context is requested).
    /// A context source should determine from <see cref="request"/>  what context to provide,
    /// and then add that context to <see cref="request"/>.
    ///
    /// This pattern allows for multiple context sources for the same type of context, without
    /// the need to "merge" the results from all sources afterward: all sources do their
    /// contribution into the same <paramref name="TContext"/> object.
    /// 
    /// This not only reduces complexity of the context provider code and source(s), but it also
    /// executes faster: generic approaches to merging context objects may involve reflection and/or
    /// serialization which can become computationally expensive. 
    ///
    /// However, this pattern does come with a responsibility to implementers of this interface: they
    /// must make sure they do only *nondestructive* updates to <paramref name="request"/>, that is:
    /// 
    ///   * only *add* to properties that contain lists and dictionaries (rather than replacing them), and
    ///   * only set or replace "scalar" (= simple type such as int, string, bool) properties when it is
    ///     certain that no other context source would set this property, or that they would set it to the
    ///     same value.
    /// 
    /// </remarks>
    /// <param name="request">The requested (context) to update</param>
    /// <param name="contextProvider">Context provider to fetch additional required context</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Instructions about how the values provided by the source can be cached</returns>
    Task<CacheInstruction> FillContextAsync(
        TContext request,
        IContextProvider contextProvider, CancellationToken cancellationToken
        );
}