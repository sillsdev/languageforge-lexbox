using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniLcm.Models;


/// <summary>
/// map like object with writing system as key and string as value
/// </summary>
// convert to a dictionary of string to string
[JsonConverter(typeof(MultiStringConverter))]
public class MultiString: IDictionary
{
    public MultiString(int capacity)
    {
        Values = new MultiStringDict(this, capacity);
    }
    public MultiString()
    {
        Values = new MultiStringDict(this);
    }

    [JsonConstructor]
    public MultiString(IDictionary<WritingSystemId, string> values)
    {
        Values = new MultiStringDict(this, values);
    }

    public virtual MultiString Copy()
    {
        return new(Values);
    }

    public override string ToString()
    {
        if (Values.Count == 0) return "{}";
        return string.Join(", ", Values.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    public virtual IDictionary<WritingSystemId, string> Values { get; }
    public virtual IDictionary<WritingSystemId, MultiStringValueMetadata> Metadata { get; } = new Dictionary<WritingSystemId, MultiStringValueMetadata>();
    public bool IsReadonly(WritingSystemId ws) => Metadata.TryGetValue(ws, out var meta) && meta.RunCount > 1;

    public string this[WritingSystemId key]
    {
        get => Values[key];
        set => Values[key] = value;
    }
    private class MultiStringDict : Dictionary<WritingSystemId, string>,
#pragma warning disable CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
        IDictionary<WritingSystemId, string>,
#pragma warning restore CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
        IDictionary
    {
        private readonly MultiString _parent;
        public MultiStringDict(MultiString parent, int capacity) : base(capacity)
        {
            _parent = parent;
        }
        public MultiStringDict(MultiString parent)
        {
            _parent = parent;
        }

        public MultiStringDict(MultiString parent, IDictionary<WritingSystemId, string> dictionary) : base(dictionary)
        {
            _parent = parent;
        }

        string IDictionary<WritingSystemId, string>.this[WritingSystemId key]
        {
            get => TryGetValue(key, out var value) ? value : string.Empty;
            set
            {
                if (_parent.IsReadonly(key))
                {
                    throw new InvalidOperationException($"Cannot set readonly property {key}");
                }
                base[key] = value;
            }
        }

        // this method gets called by json patch when applying to an object.
        // however the default dictionary implementation fails when trying to cast so we need to override it
        void IDictionary.Add(object key, object? value)
        {
            var valStr = value as string ??
                         throw new ArgumentException($"unable to convert value {value?.GetType().Name ?? "null"} to string", nameof(value));
            if (key is WritingSystemId keyWs)
            {
                Add(keyWs, valStr);
            }
            else if (key is string keyStr)
            {
                Add(keyStr, valStr);
            }
            else
            {
                throw new ArgumentException("unable to convert key to writing system id", nameof(key));
            }
        }
    }

    public void Add(string key, string value)
    {
        Values.Add(key, value);
    }

    void IDictionary.Add(object key, object? value)
    {
        ((IDictionary)Values).Add(key, value);
    }

    void IDictionary.Clear()
    {
        ((IDictionary)Values).Clear();
    }

    bool IDictionary.Contains(object key)
    {
        return ((IDictionary)Values).Contains(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return ((IDictionary)Values).GetEnumerator();
    }

    void IDictionary.Remove(object key)
    {
        ((IDictionary)Values).Remove(key);
    }

    bool IDictionary.IsFixedSize => ((IDictionary)Values).IsFixedSize;

    bool IDictionary.IsReadOnly => ((IDictionary)Values).IsReadOnly;

    object? IDictionary.this[object key]
    {
        get => ((IDictionary)Values)[key];
        set => ((IDictionary)Values)[key] = value;
    }

    ICollection IDictionary.Keys => ((IDictionary)Values).Keys;

    ICollection IDictionary.Values => ((IDictionary)Values).Values;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Values).GetEnumerator();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((IDictionary)Values).CopyTo(array, index);
    }

    int ICollection.Count => ((IDictionary)Values).Count;

    bool ICollection.IsSynchronized => ((IDictionary)Values).IsSynchronized;

    object ICollection.SyncRoot => ((IDictionary)Values).SyncRoot;
}

public class MultiStringValueMetadata
{
    public int RunCount { get; set; }
}

public static class MultiStringExtensions
{
    public static bool SearchValue(this MultiString ms, string value)
    {
        return ms.Values.Values.Any(v => v.Contains(value));
    }
}

public class MultiStringConverter : JsonConverter<MultiString>
{
    public const string MultiStringMetadataPrefix = "x-meta-";
    public override MultiString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("MultiString must be an object");
        }

        reader.Read();

        var ms = new MultiString();

        while (reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("MultiString must be an object");
            }

            var key = reader.GetString();
            reader.Read();
            if (key is null)
            {
                throw new JsonException("MultiString values must be strings");
            }

            if (key.StartsWith(MultiStringMetadataPrefix) && reader.TokenType == JsonTokenType.StartObject)
            {
                ms.Metadata.Add(key[MultiStringMetadataPrefix.Length..],
                    JsonSerializer.Deserialize<MultiStringValueMetadata>(ref reader, options) ?? new());
            }
            else
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException("MultiString value must be string");
                }
                var value = reader.GetString();
                reader.Read();
                if (value is null)
                {
                    throw new JsonException("MultiString values must be strings");
                }

                ms.Add(key, value);
            }
        }

        reader.Read();

        return ms;
    }

    public override void Write(Utf8JsonWriter writer, MultiString value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartObject();
        foreach (var kvp in value.Values)
        {
            writer.WritePropertyName(kvp.Key.Code);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }
        if (value.Metadata.Count > 0)
        {
            foreach (var kvp in value.Metadata)
            {
                var prefix = MultiStringMetadataPrefix.AsSpan();
                writer.WritePropertyName([..prefix, ..kvp.Key.Code.AsSpan()]);
                JsonSerializer.Serialize(writer, kvp.Value, options);
            }
        }
        writer.WriteEndObject();
    }
}
