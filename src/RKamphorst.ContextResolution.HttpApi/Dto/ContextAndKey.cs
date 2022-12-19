using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace RKamphorst.ContextResolution.HttpApi.Dto;

public class ContextAndKey
{
    private static readonly Regex ContextAndKeyRegex = new Regex(
        @"^(?<context>[^:\s]+)(:(?<key>[^\s]+))?$",
        RegexOptions.Compiled
    );

    
    public static ContextAndKey Parse(string contextAndKeyString)
    {
        if (TryParse(contextAndKeyString, out var result))
        {
            return result;
        }

        throw new ArgumentException(nameof(contextAndKeyString));
    }

    public static bool TryParse(string contextAndKeyString,
        [MaybeNullWhen(false)] out ContextAndKey contextAndKey)
    {
        Match m = ContextAndKeyRegex.Match(contextAndKeyString);
        if (m.Success)
        {
            contextAndKey = m.Groups["key"].Success
                ? new ContextAndKey(contextAndKeyString, m.Groups["context"].Value, m.Groups["key"].Value)
                : new ContextAndKey(contextAndKeyString, m.Groups["context"].Value, null);
            return true;
        }

        contextAndKey = null;
        return false;
    }

    private ContextAndKey(string contextAndKeyString, string contextName, string? key)
    {
        ParsedFromString = contextAndKeyString;
        ContextName = contextName;
        Key = key;
    }
    
    public string ParsedFromString { get; }
    
    public string? Key { get; }
    
    public string ContextName { get; }

    public override string ToString()
    {
        return Key != null ? $"{ContextName}:{Key}" : ContextName;
    }
}