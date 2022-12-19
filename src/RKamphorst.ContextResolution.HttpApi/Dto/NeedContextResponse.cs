namespace RKamphorst.ContextResolution.HttpApi.Dto;

public class NeedContextResponse
{
    private readonly IReadOnlyList<string>? _needContextStrings;
    private readonly IReadOnlyList<ContextAndKey>? _needContextReferences;
    private readonly IReadOnlyList<string> _badReferenceStrings = new List<string>();
    
    public IReadOnlyList<string>? NeedContext
    {
        get => _needContextStrings;
        init
        {
            if (value == null || value.Count == 0)
            {
                _needContextStrings = null;
                _needContextReferences = null;
                _badReferenceStrings = new List<string>();
                return;
            }
            
            _needContextStrings = value;

            var parsedTuples =            
                _needContextStrings.Select(s =>
                {
                    var isValid = ContextAndKey.TryParse(s, out var contextRef);
                    return (String: s, IsValid: isValid, ContextReference: contextRef);
                }).ToArray();

            if (parsedTuples.All(t => t.IsValid))
            {
                _needContextReferences = parsedTuples.Select(t => t.ContextReference!).ToList();
                _badReferenceStrings = new List<string>();
            }
            else
            {
                _needContextReferences = null;
                _badReferenceStrings = parsedTuples
                    .Where(t => !t.IsValid)
                    .Select(t => t.String).ToArray();
            }
        }
    }

    public bool IsValid => _needContextReferences != null;

    public IReadOnlyList<ContextAndKey> GetContextReferences()
    {
        return _needContextReferences ??
            throw new InvalidOperationException(
        $"{nameof(NeedContextResponse)} has invalid context reference strings"
            );
    }

    public IReadOnlyList<string> GetBadContextReferenceStrings()
    {
        return _badReferenceStrings;
    }


}