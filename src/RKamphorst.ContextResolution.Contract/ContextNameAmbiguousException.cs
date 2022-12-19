namespace RKamphorst.ContextResolution.Contract;

public class ContextNameAmbiguousException : ContextResolutionException
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contextType">No type was found for given context name</param>
    public ContextNameAmbiguousException(string contextName, IEnumerable<Type> types) : base(
        $"There are multiple context types that are available under '{contextName}'")
    {
        ContextName = contextName;
        ContextTypes = types.ToArray();
    }

    /// <summary>
    /// Context name for which there are multiple contexts
    /// </summary>
    public string ContextName { get; }
    
    /// <summary>
    /// All types that have this same name
    /// </summary>
    public Type[] ContextTypes { get; }
}