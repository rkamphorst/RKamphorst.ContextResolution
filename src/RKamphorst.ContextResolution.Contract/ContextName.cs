using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Represents the name of a context
/// </summary>
/// <remarks>
/// A context name can be any string. If it matches (case insensitive) the name of a type, or a
/// <see cref="ContextNameAttribute"/> on a type, it is automatically bound to that type.
///
/// One type can have multiple names: the name of the type itself, plus any number of
/// <see cref="ContextNameAttribute"/>s on that type. These are the context name *aliases*.
/// It is possible to refer to one context name by multiple aliases at the same time (to avoid ambiguity);
/// you can do this by specifying the aliases, separated by '|', ',' or ' ', in one string.
/// 
/// </remarks>
public readonly struct ContextName
{
    private static readonly Regex AliasSeparatorRegex = new(@"[\s,/|]+", RegexOptions.Compiled);
    
    private ContextName(Type type)
    {
        var aliases = EnumerateAliasesForType(type).ToArray();
        _type = type;
        Aliases = aliases;
    }

    private ContextName(IEnumerable<string> aliases)
    {
        var allAliases = aliases
            .SelectMany(a => AliasSeparatorRegex.Split(a)
                .Where(s => !string.IsNullOrWhiteSpace(s))
            ).ToArray();
        if (allAliases.Length == 0)
        {
            throw new ArgumentException("At least one context name alias is required", nameof(allAliases));
        }

        IReadOnlyDictionary<string, IReadOnlySet<Type>> typeMap = GetOrCreateTypeMap();

        Type[] mappedTypes =
            allAliases
                .Select(a => 
                    typeMap
                        .TryGetValue(a.Normalize().ToLowerInvariant(), out IReadOnlySet<Type>? s) ? s : null
                )
                .Aggregate(
                    new HashSet<Type>(),
                    (a, b) =>
                    {
                        if (b == null) return a;
                        if (a.Count == 0)
                        {
                            a.UnionWith(b);
                        }
                        else
                        {
                            a.IntersectWith(b);
                        }

                        return a;
                    })
                .ToArray();

        Type? type = mappedTypes.Length switch
        {
            > 1 => throw new ContextNameAmbiguousException(allAliases, mappedTypes),
            1 => mappedTypes[0],
            _ => null
        };
        _type = type;
        Aliases = type != null ? EnumerateAliasesForType(type).ToArray() : OrderCustomAliasNames(allAliases).ToArray();
    }

    private ContextName(string aliases) 
        : this(new[] { aliases }) { }

    private readonly Type? _type;

    /// <summary>
    /// Get the type that this context name is bound to
    /// </summary>
    /// <returns>The context type of this context name is bound to a type. If not, <c>null</c></returns>
    public Type? GetContextType() => _type;

    /// <summary>
    /// The aliases for this context name. 
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    private IEnumerable<string> CaseInsensitiveAliases => 
        Aliases.Select(a => a.Normalize().ToLowerInvariant());

    /// <summary>
    /// The string representation for this context name
    /// </summary>
    /// <see cref=""/>
    public string Key => string.Join("|", CaseInsensitiveAliases);

    /// <summary>
    /// Given ID (see <see cref="ContextKey.Id"/>) or result (<see cref="ContextResult.Result"/>,
    /// coerce into the expected type for this context name.
    /// </summary>
    /// <remarks>
    /// If this context name is bound to a type, tries to convert to that type.
    /// Otherwise, creates a (dynamic) object that reflects the id or result.
    /// </remarks>
    /// <param name="idOrResult">The object to coerce</param>
    /// <returns>The coerced object</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="idOrResult"/> cannot be converted into the type bound to this context name.
    /// </exception>
    public object Coerce(object? idOrResult)
    {
        var result = idOrResult ?? new { };
        if (_type != null)
        {
            if (!TryConvertToType(result, _type, out var convertedResult))
            {
                throw new ArgumentException($"Id not convertable to type {_type.Name}", nameof(idOrResult));
            }

            result = convertedResult;
        }
        else
        {
            JToken jId = JToken.FromObject(result);
            if (jId.Type != JTokenType.Object)
            {
                throw new ArgumentException($"Id is a {result.GetType().Name}, not a valid id/result object", nameof(idOrResult));
            }
        }
        return result;
    }
    
    /// <summary>
    /// Determine whether an object equals this context name. 
    /// </summary>
    /// <see cref="Equals(ContextName)"/>
    public override bool Equals(object? obj) 
        => obj is ContextName contextName && Equals(contextName);

    /// <summary>
    /// Determine whether context name equals this one
    /// </summary>
    /// <remarks>
    /// Two context names are equal if:
    /// - The type they are bound to is the same, and
    /// - Their aliases are the same, ordering matters!
    /// </remarks>
    public bool Equals(ContextName other)
        => ReferenceEquals(_type, other._type) 
            && CaseInsensitiveAliases.SequenceEqual(other.CaseInsensitiveAliases);

    /// <summary>
    /// Calculate hash code for this context name
    /// </summary>
    /// <remarks>
    /// The hash code is based on:
    /// - the type bound to this context name, if any
    /// - the aliases for this context name, in order
    /// </remarks>
    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(_type);
        foreach (var a in CaseInsensitiveAliases)
        {
            h.Add(a);
        }
        return h.ToHashCode();
    }
    
    /// <summary>
    /// Determine whether this context name *matches* another one
    /// </summary>
    /// <remarks>
    /// This context name matches the other if all the aliases that the other context name has, are also in this context
    /// name's aliases list (order does NOT matter here)
    /// </remarks>
    public bool Matches(ContextName other)
    {
        HashSet<string> myAliases = CaseInsensitiveAliases.ToHashSet();
        return other.CaseInsensitiveAliases.All(myAliases.Contains);
    }

    /// <summary>
    /// Create a string representation for this context name. Returns what is in <see cref="Key"/>
    /// </summary>
    public override string ToString() => Key;

    /// <summary>
    /// Create from string by casting
    /// </summary>
    /// <param name="aliases">
    ///     A string with one or more aliases.
    ///     If more than one, they need to be separated by '|', ',' or ' '
    /// </param>
    public static explicit operator ContextName(string aliases) => new(aliases);

    /// <summary>
    /// Create from type by casting
    /// </summary>
    /// <remarks>
    /// A context name bound to given type is created; its aliases are the type's name and all the
    /// <see cref="ContextNameAttribute"/>s on that type.
    /// </remarks>
    public static explicit operator ContextName(Type type) => new(type);

    /// <summary>
    /// Cast to string
    /// </summary>
    /// <see cref="ToString"/>
    public static explicit operator string(ContextName contextName) => contextName.ToString();

    /// <summary>
    /// Operator overload: ==
    /// </summary>
    /// <see cref="Equals(ContextName)"/>
    public static bool operator ==(ContextName left, ContextName right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Operator overload: !=
    /// </summary>
    /// <see cref="Equals(ContextName)"/>
    public static bool operator !=(ContextName left, ContextName right)
    {
        return !(left == right);
    }
    
    private static IEnumerable<string> EnumerateAliasesForType(Type type)
    {
        var shortName = type.FullName![(type.FullName!.LastIndexOf('.') + 1)..];
        
        yield return shortName;

        foreach (string name in OrderCustomAliasNames(
                    type.GetCustomAttributes<ContextNameAttribute>().Select(a => a.Name)
                ))
        {
            yield return name;
        }
    }

    private static IEnumerable<string> OrderCustomAliasNames(IEnumerable<string> aliasNames)
    {
        return aliasNames.OrderBy(a => a.Length)
            .ThenBy(a => a);
    }

    #region Type conversion
    
    public static bool TryConvertToType(object toConvert, Type toType, [MaybeNullWhen(false)] out object converted)
    {
        if (toType.IsInstanceOfType(toConvert))
        {
            converted = toConvert;
            return true;
        }

        var jTokenToConvert = toConvert as JToken ?? JToken.FromObject(toConvert);
        converted = jTokenToConvert switch
        {
            { Type: JTokenType.Object } tok => tok.ToObject(toType),
            _ => null
        };

        return converted != null;
    } 
    #endregion
    
    #region Context Name to Context Type mapping
    
    private static IDictionary<string, IReadOnlySet<Type>>? _contextTypeMap;

    private static void UpdateTypeMapWithAssemblies(
        IDictionary<string, IReadOnlySet<Type>> typeMap, IEnumerable<Assembly> assemblies
    )
    {
        Type[] allPossibleContextTypes = // get all the assemblies
            assemblies
                .SelectMany(assembly => assembly.DefinedTypes.Select(i => i.AsType()))
                .Where(type =>
                    // the following corresponds to 'where TContext: class, new()' 
                    type.IsClass 
                    && !type.IsAbstract && type.IsPublic 
                    && type.GetConstructor(Type.EmptyTypes) != null
                    )
                .ToArray();

        IEnumerable<IGrouping<string, (string Name, Type Type)>> typeGroups =
            allPossibleContextTypes // keep only types that are valid contexts 
                .SelectMany(type => 
                    EnumerateAliasesForType(type)
                        .Select(name => (Name: name.Normalize().ToLowerInvariant(), Type: type))
                ) // make a big list (upper cased) of names for each type
                .Where(t => !string.IsNullOrWhiteSpace(t.Name)) // discard types that have no suitable name
                .Distinct() // make sure we have only unique tuples
                .GroupBy(t => t.Name); // now, some names may have the same name, so we group them

        foreach (IGrouping<string, (string Name, Type Type)> g in typeGroups)
        {
            var newSet = new HashSet<Type>(g.Select(t => t.Type));
            if (typeMap.TryGetValue(g.Key, out IReadOnlySet<Type>? existingSet))
            {
                newSet.UnionWith(existingSet);
            }
            typeMap[g.Key] = newSet;
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<Type>> GetOrCreateTypeMap()
    {
        if (_contextTypeMap != null)
        {
            return new ReadOnlyDictionary<string, IReadOnlySet<Type>>(_contextTypeMap);
        }

        IEnumerable<Assembly> allAssemblies = new[] { Assembly.GetEntryAssembly() }
            .Concat(AppDomain.CurrentDomain.GetAssemblies())
            .Concat(
                Assembly.GetEntryAssembly()?.GetReferencedAssemblies().Select(Assembly.Load)
                ?? Enumerable.Empty<Assembly>()
            )
            .Where(a => a != null)
            .Select(a => a!)
            .Distinct();

        var newTypeMap = new Dictionary<string, IReadOnlySet<Type>>();
        UpdateTypeMapWithAssemblies(newTypeMap, allAssemblies);
        _contextTypeMap = newTypeMap;

        // if a new assembly is loaded after this, update the type map
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
            UpdateTypeMapWithAssemblies(_contextTypeMap, new[] { args.LoadedAssembly });

        return new ReadOnlyDictionary<string, IReadOnlySet<Type>>(_contextTypeMap);
    }

    #endregion
}