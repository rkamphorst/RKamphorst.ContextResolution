﻿namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Exception that is thrown when no context source could be found that provides a given context type
/// </summary>
public class ContextSourceNotFoundException : ContextResolutionException
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contextType">The context type for which no context source was available</param>
    public ContextSourceNotFoundException(ContextName contextName) : base(
        $"No source found for context '{contextName}'")
    {
        ContextName = contextName;
    }

    /// <summary>
    /// Type of the context for which no source could be found
    /// </summary>
    public ContextName ContextName { get; }
}