namespace RKamphorst.ContextResolution.Contract.Test.Stubs;

[ContextName("alias-1")]
[ContextName("alias-2")]
public class StubContextWithAliases
{
    public string? Property { get; set; }
    
    public string? Property2 { get; set; }
}