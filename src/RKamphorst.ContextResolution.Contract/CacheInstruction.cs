using System.Text.RegularExpressions;

namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Instruction for cache as to how to cache and how long to cache
/// </summary>
/// <remarks>
/// Currently, this instruction only indicates how long to cache; this is interpreted as cache period for both
/// local and distributed cache.
/// </remarks>
public readonly struct CacheInstruction
{

    private static readonly Regex InstructionRegex = new(
        @"^\s*((?<transient>transient)|(?<expiration>(?<num>[\d]+|\d*\.\d+)\s*(?<unit>s|sec|second|seconds|m|min|minute|minutes|h|hour|hours|d|day|days)?))\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );
    
    /// <summary>
    /// Parse cache instruction
    /// </summary>
    /// <remarks>
    /// The supported cache instructions are:
    /// - "transient": do not cache
    /// - [number] [s|sec|seconds], example: "15 sec" (number of seconds)
    /// - [number] [m|min|minutes], example: "15 min" (number of minutes)
    /// - [number] [h|hour|hours], example: "15 h" (number of hours)
    /// - [number] [d|day|days], example: "3 days" (number of days)
    /// </remarks>
    /// <param name="instruction">Instruction to parse</param>
    /// <param name="cacheInstruction">Resulting cache instruction</param>
    /// <returns>Whether parsing succeeded</returns>
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

    /// <summary>
    /// Parse cache instruction
    /// </summary>
    /// <remarks>
    /// Uses <see cref="TryParse"/> to parse a cache instruction and throws an exception if parsing failed.
    /// </remarks>
    /// <param name="instruction">Instruction to parse</param>
    /// <returns>The parsed cache instruction</returns>
    /// <exception cref="ArgumentException">Thrown if parsing failed</exception>
    public static CacheInstruction Parse(string instruction)
        => TryParse(instruction, out CacheInstruction? result)
            ? result!.Value
            : throw new ArgumentException($"Not a valid cache instruction: '{instruction}'", nameof(instruction));

    /// <summary>
    /// Combine multiple cache instructions into one
    /// </summary>
    /// <remarks>
    /// Multiple cache instructions are safely combined into one. For example, if two cache instructions indicate
    /// different expiration periods, the shortest is taken.
    /// </remarks>
    /// <param name="instructions">Cache instructions to combine</param>
    /// <returns>Resulting cache instructions</returns>
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
    
    /// <summary>
    /// Create cache instruction from a timespan
    /// </summary>
    /// <param name="timeSpan">Timespan to create cache instruction from</param>
    /// <returns>Cache instruction with cache expiration time </returns>
    public static CacheInstruction FromTimeSpan(TimeSpan timeSpan) 
        => timeSpan > TimeSpan.Zero 
            ? new CacheInstruction($"{(int) timeSpan.TotalSeconds} seconds", true, true, timeSpan) 
            : Transient;

    /// <summary>
    /// Transient cache instruction
    /// </summary>
    /// <remarks>
    /// This cache instruction indicates that no caching should be done.
    /// </remarks>
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

    /// <summary>
    /// Creates a string representation of the cache instruction
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString() => _instruction;

    /// <summary>
    /// Cast a <see cref="TimeSpan"/> to a <see cref="CacheInstruction"/>
    /// </summary>
    /// <seealso cref="FromTimeSpan"/>
    public static explicit operator CacheInstruction(TimeSpan timeSpan) => FromTimeSpan(timeSpan);

    /// <summary>
    /// Cast a string to a cache instruction
    /// </summary>
    /// <seealso cref="Parse"/>
    public static explicit operator CacheInstruction(string instruction) => Parse(instruction);

    /// <summary>
    /// Cast a cache instruction to a string
    /// </summary>
    /// <seealso cref="ToString"/>
    public static explicit operator string(CacheInstruction instruction) => instruction.ToString();

    /// <summary>
    /// Get remaining expiration time, given some time has passed already, for local cache
    /// </summary>
    /// <param name="age">Period of time that has expired since the item was created</param>
    /// <returns>Remaining expiration time</returns>
    public TimeSpan GetLocalExpirationAtAge(TimeSpan age)
        => this switch
        {
            { _isLocallyCacheable: false } => TimeSpan.Zero,
            _ => _expiration.HasValue && _expiration.Value - age > TimeSpan.Zero ? _expiration.Value - age : TimeSpan.Zero
        };

    /// <summary>
    /// Get remaining expiration time, given some time has passed already, for distributed cache
    /// </summary>
    /// <param name="age">Period of time that has expired since the item was created</param>
    /// <returns>Remaining expiration time</returns>
    public TimeSpan GetDistributedExpirationAtAge(TimeSpan age)
        => this switch
        {
            { _isDistributedCacheable: false } => TimeSpan.Zero,
            _ => _expiration.HasValue && _expiration - age > TimeSpan.Zero ? _expiration.Value - age : TimeSpan.Zero
        };
    
}