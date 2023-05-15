namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Exception thrown when a circular dependency between contexts is detected
/// </summary>
public class ContextCircularDependencyException : ContextResolutionException
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contextKeyPath">
    /// The context path: the traversed context dependencies. See <see cref="ContextKeyPath"/>
    /// </param>
    /// <param name="requestedContextKey">
    /// The name of the context that was requested and caused this exception to be thrown.
    /// See <see cref="RequestedContextKey"/>
    /// </param>
    public ContextCircularDependencyException(ContextKey[] contextKeyPath, ContextKey requestedContextKey) 
        : base($"Circular dependency detected: {string.Join("->", contextKeyPath)}->{requestedContextKey}")
    {
        ContextKeyPath = contextKeyPath;
        RequestedContextKey = requestedContextKey;
    }

    /// <summary>
    /// The context path: the traversed context dependencies.
    /// </summary>
    public ContextKey[] ContextKeyPath { get; }
    
    /// <summary>
    /// The context that was requested and caused this exception to be thrown.
    /// </summary>
    public ContextKey RequestedContextKey { get; }
}