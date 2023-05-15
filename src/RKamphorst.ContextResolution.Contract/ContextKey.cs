using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Key that uniquely identifies a context
/// </summary>
/// <remarks>
/// A context key essentially consists of two parts:
///
/// - The name of the context. See <see cref="ContextName"/>
/// - An identifying part. This can be any object (not a primitive value) that is of a reference type.
///
/// The context key is meant for use as key in e.g. dictionaries and caches.
/// See <see cref="Equals"/> for a definition when two context keys are the same.
///
/// Note that for the sake of understandability and performance it is best to avoid enumerable properties in.
///
/// </remarks>
public readonly struct ContextKey
{
    
    /// <summary>
    /// Create a context key from a typed object
    /// </summary>
    /// <remarks>
    /// The ID is the typed object, the context name is determined from the type of that object.
    /// </remarks>
    /// <param name="id">The typed object to create the ID from</param>
    /// <typeparam name="TContext"></typeparam>
    /// <returns>The created context key</returns>
    public static ContextKey FromTypedContext<TContext>(TContext? id = null) where TContext : class, new()
        => new((ContextName)typeof(TContext), id);

    /// <summary>
    /// Create a context key from a name and an object
    /// </summary>
    /// <param name="name">Context name</param>
    /// <param name="id">Context Id (optional). If not given, an empty object will be used as id</param>
    /// <remarks>
    /// The <paramref name="name"/> is converted to a <see cref="ContextName"/>.
    /// The <paramref name="id"/> can be <c>null</c>; in that case it will be substituted with an empty object.
    /// </remarks>
    /// <returns>The created context key</returns>
    public static ContextKey FromNamedContext(string name, object? id = null) 
        => new((ContextName)name, id);

    /// <summary>
    /// Try to parse a context key into a string
    /// </summary>
    /// <param name="stringKey">String representation of a context key</param>
    /// <param name="result">The resulting context key</param>
    /// <returns>Whether parsing the context key succeeded</returns>
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

    /// <summary>
    /// Create a context key from a string, throw an exception on failure
    /// </summary>
    /// <param name="stringKey">The string to parse into a context key</param>
    /// <returns>The parsed context key</returns>
    /// <exception cref="ArgumentException">Thrown if parsing failed</exception>
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

    /// <summary>
    /// The context's name
    /// </summary>
    public ContextName Name { get; }

    /// <summary>
    /// String representation of the context key
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// The context's ID
    /// </summary>
    /// <remarks>
    /// The context ID can be of any type, except enumerable or primitive type
    /// </remarks>
    public object Id { get; }

    /// <summary>
    /// Compare to another object, possibly a context key
    /// </summary>
    /// <remarks>
    /// Two context keys are the same if:
    /// - Their names are the same (see <see cref="ContextName"/>)
    /// - Their <see cref="Id"/>s are the same.
    ///   Ids are the same if they have properties that have the same values.
    ///   Property names are matched case insensitively, and if the object has properties that are enumerables (e.g. arrays,
    ///   lists, dictionaries), they are also matched regardless to ordering of those enumerables: as long as they contain
    ///   the same items, they are the same.</remarks>
    /// <param name="obj">Object to compare to</param>
    /// <returns>Whether the objects are equal</returns>
    public override bool Equals(object? obj) =>
        obj switch
        {
            ContextKey otherKey => otherKey.Key.Equals(Key),
            _ => false
        };

    /// <summary>
    /// Operator overload: ==
    /// </summary>
    /// <seealso cref="Equals"/>
    public static bool operator ==(ContextKey left, ContextKey right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Operator overload: !=
    /// </summary>
    /// <seealso cref="Equals"/>
    public static bool operator !=(ContextKey left, ContextKey right)
    {
        return !(left == right);
    }
    
    /// <summary>
    /// Calculates this context key's hash key
    /// </summary>
    /// <remarks>The hash key is based on what is in <see cref="Key"/></remarks>
    public override int GetHashCode() => HashCode.Combine(Key);

    /// <summary>
    /// Create a string representation for this context key
    /// </summary>
    /// <remarks>
    /// Returns what is in <see cref="Key"/>
    /// </remarks>
    public override string ToString() => Key;

    /// <summary>
    /// Cast to string
    /// </summary>
    /// <seealso cref="Tostring"/>
    public static explicit operator string(ContextKey contextKey) => contextKey.ToString();
    
    /// <summary>
    /// Cast from string
    /// </summary>
    /// <seealso cref="FromStringKey"/>
    public static explicit operator ContextKey(string stringKey) => FromStringKey(stringKey);

    #region Key serialization

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
            throw new NotSupportedException();
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
