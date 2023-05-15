namespace RKamphorst.ContextResolution.HttpApi.Dto;

public class RequestWithContextDto<TRequest>
{
    public TRequest? Request { get; init; }
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}