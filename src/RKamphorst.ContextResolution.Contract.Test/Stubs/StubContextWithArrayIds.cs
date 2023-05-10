namespace RKamphorst.ContextResolution.Contract.Test.Stubs;

[ContextName("array-ids")]
public class StubContextWithArrayIds
{
    public string[] Id { get; set; }

    public string[][] Id2 { get; set; }
    
    public StubContextWithArrayIds[] Id3 { get; set; }

}