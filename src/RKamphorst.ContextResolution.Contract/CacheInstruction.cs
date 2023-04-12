using System.Text.RegularExpressions;

namespace RKamphorst.ContextResolution.Contract;

public readonly struct CacheInstruction
{

    private static readonly Regex InstructionRegex = new(
        @"^\s*((?<transient>transient)|(?<expiration>(?<num>[\d]+|\d*\.\d+)\s*(?<unit>s|sec|second|seconds|m|min|minute|minutes|h|hour|hours|d|day|days)?))\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    public static bool TryParse(string instruction, out CacheInstruction? cacheInstruction)
    {
        Match m = InstructionRegex.Match(instruction);
        if (!m.Success)
        {
            cacheInstruction = null;
            return false;
        }

        if (m.Groups["transient"].Success)
        {
            cacheInstruction = Transient;
            return true;
        }

        // if we arrive here, cache instruction must be an expiration time
        
        var num = double.Parse(m.Groups["num"].Value);
        var unit = m.Groups["unit"].Success ? m.Groups["unit"].Value.ToLowerInvariant() : "s";
        switch (unit)
        {
            case "d":
            case "day":
            case "days":
                cacheInstruction = FromTimeSpan(TimeSpan.FromDays(num));
                break;
            case "h":
            case "hour":
            case "hours":
                cacheInstruction = FromTimeSpan(TimeSpan.FromHours(num));
                break;
            case "m":
            case "min":
            case "minute":
            case "minutes":
                cacheInstruction = FromTimeSpan(TimeSpan.FromMinutes(num));
                break;
            default:
                cacheInstruction = FromTimeSpan(TimeSpan.FromSeconds(num));
                break;
        }

        return true;
    }

    public static CacheInstruction Parse(string instruction)
        => TryParse(instruction, out CacheInstruction? result)
            ? result!.Value
            : throw new ArgumentException($"Not a valid cache instruction: '{instruction}'", nameof(instruction));

    public static CacheInstruction Combine(IEnumerable<CacheInstruction> instructions)
    {
        CacheInstruction[] instructionsArray = instructions.ToArray();
        var isCacheable = instructionsArray.All(i => i._isLocallyCacheable || i._isDistributedCacheable);
        if (!isCacheable)
        {
            return Transient;
        }

        TimeSpan expiration = instructionsArray.Min(i => i._expiration) ?? TimeSpan.Zero;
        return FromTimeSpan(expiration);
    }
    
    public static CacheInstruction FromTimeSpan(TimeSpan timeSpan) 
        => timeSpan > TimeSpan.Zero 
            ? new CacheInstruction($"{(int) timeSpan.TotalSeconds} seconds", true, true, timeSpan) 
            : Transient;

    public static CacheInstruction Transient { get; } = new("transient", false, false, TimeSpan.Zero);

    private CacheInstruction(
        string instruction,
        bool isLocallyCacheable, 
        bool isDistributedCacheable,
        TimeSpan? expiration
        )
    {
        _instruction = instruction;
        _isLocallyCacheable = isLocallyCacheable;
        _isDistributedCacheable = isDistributedCacheable;
        _expiration = expiration;
    }

    private readonly string _instruction;
    private readonly bool _isLocallyCacheable;

    private readonly bool _isDistributedCacheable;

    private readonly TimeSpan? _expiration;

    public override string ToString() => _instruction;

    public static explicit operator CacheInstruction(TimeSpan timeSpan) => FromTimeSpan(timeSpan);

    public static explicit operator CacheInstruction(string instruction) => Parse(instruction);

    public static explicit operator string(CacheInstruction instruction) => instruction.ToString();
    
    

    
    public TimeSpan GetLocalExpirationAtAge(TimeSpan age)
        => this switch
        {
            { _isLocallyCacheable: false } => TimeSpan.Zero,
            _ => _expiration.HasValue && _expiration.Value - age > TimeSpan.Zero ? _expiration.Value - age : TimeSpan.Zero
        };

    public TimeSpan GetDistributedExpirationAtAge(TimeSpan age)
        => this switch
        {
            { _isDistributedCacheable: false } => TimeSpan.Zero,
            _ => _expiration.HasValue && _expiration - age > TimeSpan.Zero ? _expiration.Value - age : TimeSpan.Zero
        };
    
}