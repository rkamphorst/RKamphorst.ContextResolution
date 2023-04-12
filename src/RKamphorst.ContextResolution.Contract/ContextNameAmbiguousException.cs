namespace RKamphorst.ContextResolution.Contract;

public class ContextNameAmbiguousException : ContextResolutionException
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="aliases">No type was found for given context name aliases</param>
    public ContextNameAmbiguousException(string[] aliases, IEnumerable<Type> types) : base(
        $"There are multiple context types that are available under '{string.Join(",", aliases)}'")
    {
        Aliases = aliases;
        ContextTypes = types.ToArray();
    }

    /// <summary>
    /// Context name aliases for which there are multiple contexts
    /// </summary>
    public string[] Aliases { get; }
    
    /// <summary>
    /// All types that match <see cref="Aliases"/>
    /// </summary>
    public Type[] ContextTypes { get; }
}