namespace RKamphorst.ContextResolution.HttpApi.Dto;

public class RequestWithContext<TParameter>
{
    public TParameter? Parameter { get; init; }
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}
