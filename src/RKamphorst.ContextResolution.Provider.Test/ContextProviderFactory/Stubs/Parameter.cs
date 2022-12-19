﻿namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

public class Parameter
{
    public bool IsContextANeeded { get; init; }
    public bool IsContextBNeededForContextA { get; init; }

    public bool IsContextBNeeded { get; init; }
    public bool IsContextANeededForContextB { get; init; }
}