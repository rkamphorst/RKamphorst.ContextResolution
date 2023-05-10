using System.Collections;
using System.Text;
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
        => Serialize(new Dictionary<string, object> { [name.Key] = id });

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

    private static string Serialize(object? o) =>  JsonConvert.SerializeObject(o, ContextKeySerializerSettings);
    
    private static readonly JsonSerializerSettings ContextKeySerializerSettings = new()
    {
        ContractResolver = new ContextKeyContractResolver(),
        Converters = new List<JsonConverter> { new SortedArrayConverter() },
        DefaultValueHandling = DefaultValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
    };


    private class SortedArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return !typeof(string).IsAssignableFrom(objectType)
                    && (!typeof(JToken).IsAssignableFrom(objectType) ||
                        typeof(JArray).IsAssignableFrom(objectType)
                    ) &&
                    (typeof(IEnumerable).IsAssignableFrom(objectType) ||
                        typeof(IDictionary).IsAssignableFrom(objectType));
        }

        #region Disabled read

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            switch (value)
            {
                case string:
                case JToken and not JArray:
                case not IDictionary and not IEnumerable:
                    throw new InvalidOperationException(
                        $"${nameof(SortedArrayConverter)} does not support " +
                        $"type {(value?.GetType().Name ?? "[null]")}"
                    );
                
                case IDictionary dict:
                    SortAndWriteDict(dict);
                    break;
                    
                
                case IEnumerable enu:
                    SortAndWriteArray(enu);
                    break;
            }

            void SortAndWriteDict(IDictionary dict)
            {
                var propertyArray = new List<(string Name, string Value)>();
                
                foreach (DictionaryEntry e in dict)
                {
                    var propertyName =
                        serializer.ContractResolver is DefaultContractResolver resolver
                            ? resolver.GetResolvedPropertyName((string)e.Key)
                            : (string)e.Key;
                    
                    var b = new StringBuilder();
                    using var w = new StringWriter(b);
                    serializer.Serialize(w, e.Value);
                    var propertyValue = b.ToString();
                    
                    propertyArray.Add((Name: propertyName, Value: propertyValue));
                }

                propertyArray.Sort(
                    (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));
                
                writer.WriteStartObject();
                foreach (var (propertyName, propertyValue) in propertyArray)
                {
                    writer.WritePropertyName(propertyName);
                    writer.WriteRawValue(propertyValue);
                }
                writer.WriteEndObject();
            }

            void SortAndWriteArray(IEnumerable enu)
            {
                var jsonArray =
                    enu.Cast<object?>()
                        .Select(o =>
                        {
                            var b = new StringBuilder();
                            using var w = new StringWriter(b);
                            serializer.Serialize(w, o);
                            return b.ToString();
                        })
                        .OrderBy(s => s)
                        .ToArray();

                writer.WriteStartArray();
                foreach (var json in jsonArray)
                {
                    writer.WriteRawValue(json);
                }

                writer.WriteEndArray();
            }
        }
    }

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
