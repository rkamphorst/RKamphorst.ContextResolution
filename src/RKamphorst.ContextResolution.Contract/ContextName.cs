using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace RKamphorst.ContextResolution.Contract;

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
                .Select(a => typeMap.TryGetValue(a, out IReadOnlySet<Type>? s) ? s : null)
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

    public Type? GetContextType() => _type;

    public IReadOnlyList<string> Aliases { get; }

    public string Key => string.Join("|", Aliases);

    public object Coerce(object? idOrResult)
    {
        var result = idOrResult ?? new { };
        if (_type != null)
        {
            if (!TryConvertToType(result, _type, out var convertedResult))
            {
                throw new ArgumentException($"Id or result ot convertible to type {_type.Name}", nameof(idOrResult));
            }

            result = convertedResult;
        }
        else
        {
            JToken jId = JToken.FromObject(result);
            if (jId.Type != JTokenType.Object)
            {
                throw new ArgumentException($"Id or result is a {result.GetType().Name}, not a valid id/result object", nameof(idOrResult));
            }
        }
        return result;
    }
    
    public override bool Equals(object? obj) 
        => obj is ContextName contextName && Equals(contextName);

    public bool Equals(ContextName other) 
        => ReferenceEquals(_type, other._type) && Aliases.SequenceEqual(other.Aliases);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(_type);
        foreach (var a in Aliases)
        {
            h.Add(a);
        }
        return h.ToHashCode();
    }
    
    public bool Matches(ContextName other) => other.Aliases.All(Aliases.Contains);

    public override string ToString() => Key;

    public static explicit operator ContextName(string aliases) => new(aliases);
    
    public static explicit operator ContextName(Type type) => new(type);

    public static explicit operator string(ContextName contextName) => contextName.ToString();

    public static bool operator ==(ContextName left, ContextName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContextName left, ContextName right)
    {
        return !(left == right);
    }
    
    private static IEnumerable<string> EnumerateAliasesForType(Type type)
    {
        var shortName = type.FullName![(type.FullName.LastIndexOf('.') + 1)..];
        if (shortName != type.FullName)
        {
            yield return shortName;
        }

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
                .SelectMany(type => EnumerateAliasesForType(type).Select(name => (Name: name, Type: type))
                ) // make a big list of full names and short names for each type
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