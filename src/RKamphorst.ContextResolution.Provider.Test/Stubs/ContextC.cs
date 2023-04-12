using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.Stubs;

[ContextName("context-c-alias")]
[ContextName("other-context-c-alias")]
[ContextName("ambiguous")]
public class ContextC
{
    public string? Id { get; set; }
    public ContextA? A { get; set; }

    public ContextB? B { get; set; }
    
}