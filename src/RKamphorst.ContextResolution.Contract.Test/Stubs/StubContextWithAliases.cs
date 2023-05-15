namespace RKamphorst.ContextResolution.Contract.Test.Stubs;

[ContextName("alias-1")]
[ContextName("alias-2")]
public class StubContextWithAliases
{
    public string? AProperty { get; set; }
    
    public string? BProperty { get; set; }
}