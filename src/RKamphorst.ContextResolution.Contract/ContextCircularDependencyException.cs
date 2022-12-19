namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Exception thrown when a circular dependency between contexts is detected
/// </summary>
public class ContextCircularDependencyException : ContextResolutionException
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contextPath">
    /// The context path: the traversed context dependencies. See <see cref="ContextPath"/>
    /// </param>
    /// <param name="requestedContext">
    /// The name of the context that was requested and caused this exception to be thrown.
    /// See <see cref="RequestedContext"/>
    /// </param>
    public ContextCircularDependencyException(Type[] contextPath, Type requestedContext) : base(
        $"Circular dependency detected: {string.Join("->", contextPath.Select(t => t.Name))}->{requestedContext.Name}"
    )
    {
        ContextPath = contextPath;
        RequestedContext = requestedContext;
    }

    /// <summary>
    /// The context path: the traversed context dependencies.
    /// </summary>
    public Type[] ContextPath { get; }
    
    /// <summary>
    /// The context that was requested and caused this exception to be thrown.
    /// </summary>
    public Type RequestedContext { get; }
}