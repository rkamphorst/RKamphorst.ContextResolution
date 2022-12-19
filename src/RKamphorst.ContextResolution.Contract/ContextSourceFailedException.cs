namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Exception thrown when a context source fails while fetching context
/// </summary>
public class ContextSourceFailedException : ContextResolutionException
{
    public ContextSourceFailedException(string? contextSource, Exception ex) 
        : base($"Context source '{contextSource}' failed with exception", ex)
    {
        ContextSource = contextSource;
    }
    
    public string? ContextSource { get; }
}