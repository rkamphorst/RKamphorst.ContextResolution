using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider.Test.Stubs;

[ContextName("ambiguous")]
public class ContextD
{
    public string? Id { get; set; }
    
    public ContextA? A { get; set; }

    public ContextB? B { get; set; }
}