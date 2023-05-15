namespace RKamphorst.ContextResolution.Contract.Test.Stubs;

[ContextName("alias-2")]
[ContextName("alias-3")]
public class StubContextWithAliases2
{
    public string Property { get; set; }
    
    public string AProperty { get; set; }
}