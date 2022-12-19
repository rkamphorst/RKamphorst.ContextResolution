namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Exception that is thrown when a context could not be resolved.
/// </summary>
/// <remarks>
/// This is the base exception for cases where the context could not be resolved.
/// More specific exceptions are thrown in specific cases.
/// </remarks>
/// <seealso cref="ContextSourceNotFoundException"/>
/// <seealso cref="ContextCircularDependencyException"/>
public class ContextResolutionException : Exception
{
    protected ContextResolutionException(string message) : base(message)
    {
        
    }

    protected ContextResolutionException(string message, Exception x) : base(message, x)
    {
        
    }
}