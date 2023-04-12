namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Exception thrown when a context source fails while fetching context
/// </summary>
public class ContextSourceFailedException : ContextResolutionException
{
    public ContextSourceFailedException(object contextSource, Exception ex) 
        : base($"Context source '{contextSource.GetType().FullName}' failed with exception", ex)
    {
        ContextSource = contextSource;
    }
    
    public object ContextSource { get; }
}