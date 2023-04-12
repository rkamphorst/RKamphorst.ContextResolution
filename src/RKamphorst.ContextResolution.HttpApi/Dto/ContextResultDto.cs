namespace RKamphorst.ContextResolution.HttpApi.Dto;

public class ContextResultDto
{
    public object? Result { get; set; }
    
    public string? CacheInstruction { get; set; }
}