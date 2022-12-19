namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Thrown if no type could be found for given name
/// </summary>
/// <remarks>
/// This can happen if the context is indicated by its name rather than by its type
/// such as in <see cref="IContextProvider."/>
/// </remarks>
public class ContextNameNotFoundException : ContextResolutionException
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contextType">No type was found for given context name</param>
    public ContextNameNotFoundException(string contextName) : base(
        $"No context type found for context '{contextName}'")
    {
        ContextName = contextName;
    }

    /// <summary>
    /// Context for which no type could be found
    /// </summary>
    public string ContextName { get; }
}