using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RKamphorst.ContextResolution.Contract;

public readonly struct ContextKey
{
    public static ContextKey FromTypedContext<TContext>(TContext? id = null) where TContext : class, new()
        => new((ContextName)typeof(TContext), id);

    public static ContextKey FromNamedContext(string name, object? id = null) 
        => new((ContextName)name, id);

    public static bool TryParse(string stringKey, out ContextKey? result)
    {
        try
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringKey);
            if (dict is { Count: 1 })
            {
                result =
                    dict
                        .Select(kvp => new ContextKey((ContextName)kvp.Key, kvp.Value))
                        .Single();
                return true;
            }
        }
        catch (JsonReaderException)
        {
            /* skip */
        }

        result = null;
        return false;
    }

    public static ContextKey FromStringKey(string stringKey)
    {
        if (!TryParse(stringKey, out ContextKey? result))
        {
            throw new ArgumentException("Not a valid context key", nameof(stringKey));
        }

        return result!.Value;
    }
    
    private static string CreateStringKey(ContextName name, object id)
        => JsonConvert.SerializeObject(new Dictionary<string, object> { [name.Key] = id }, ContextKeySerializerSettings);

    private ContextKey(ContextName name, object? id)
    {
        Name = name;
        Id = name.Coerce(id);
        Key = CreateStringKey(name, Id);
    }

    public ContextName Name { get; }

    public string Key { get; }
    
    public object Id { get; }

    public override bool Equals(object? obj) =>
        obj switch
        {
            ContextKey otherKey => otherKey.Key.Equals(Key),
            _ => false
        };

    public static bool operator ==(ContextKey left, ContextKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContextKey left, ContextKey right)
    {
        return !(left == right);
    }
    
    public override int GetHashCode() => HashCode.Combine(Key);

    public override string ToString() => Key;

    public static explicit operator string(ContextKey contextKey) => contextKey.ToString();
    
    public static explicit operator ContextKey(string stringKey) => FromStringKey(stringKey);

    

    #region JSON serializer settings

    private static readonly JsonSerializerSettings ContextKeySerializerSettings = new()
    {
        ContractResolver = new ContextKeyContractResolver(),
        DefaultValueHandling = DefaultValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
        
        
    };

    private class ContextKeyContractResolver : DefaultContractResolver
    {
        public ContextKeyContractResolver()
        {
            NamingStrategy = new CamelCaseNamingStrategy();
        }
        
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> @base = base.CreateProperties(type, memberSerialization);
            List<JsonProperty> ordered = @base.OrderBy(p => p.PropertyName).ToList();
            return ordered;
        }

    }

    

    #endregion
}