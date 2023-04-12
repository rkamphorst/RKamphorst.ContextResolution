using Newtonsoft.Json;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.HttpApi.Dto;

public class NeedContextDto
{
    private readonly IReadOnlyCollection<string>? _requestContext;
    private readonly IReadOnlySet<ContextKey> _requestContextKeys = new HashSet<ContextKey>();

    private readonly IReadOnlyCollection<string>? _requireContext;
    private readonly IReadOnlySet<ContextKey> _requireContextKeys = new HashSet<ContextKey>();


    [JsonProperty]
    public IReadOnlyCollection<string>? RequestContext
    {
        get => _requestContext;
        init
        {
            _requestContext = value;
            _requestContextKeys = value?.Select(v => (ContextKey)v).ToHashSet() ?? new HashSet<ContextKey>();
        }
    }

    [JsonProperty]
    public IReadOnlyCollection<string>? RequireContext
    {
        get => _requireContext;
        init
        {
            _requireContext = value;
            _requireContextKeys = value?.Select(v => (ContextKey)v).ToHashSet() ?? new HashSet<ContextKey>();
        }
    }

    public bool IsValid => (_requestContext != null) || _requireContext != null;

    public IReadOnlySet<ContextKey> GetRequestedContextKeys() => _requestContextKeys;

    public IReadOnlySet<ContextKey> GetRequiredContextKeys() => _requireContextKeys;

}