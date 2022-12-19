using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

[ContextName("ambiguous")]
public class ContextD
{
    public ContextA? A { get; set; }

    public ContextB? B { get; set; }
}