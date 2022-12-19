using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.ContextProviderFactory.Stubs;

[ContextName("blabla")]
[ContextName("ambiguous")]
public class ContextC
{
    public ContextA? A { get; set; }

    public ContextB? B { get; set; }
}