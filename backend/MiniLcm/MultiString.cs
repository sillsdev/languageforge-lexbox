using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniLcm;


/// <summary>
/// map like object with writing system as key and string as value
/// </summary>
// convert to a dictionary of string to string
[JsonConverter(typeof(MultiStringConverter))]
public class MultiString: IDictionary
{
    public MultiString()
    {
        _values = new MultiStringDict();
    }

    [JsonConstructor]
    public MultiString(IDictionary<WritingSystemId, string> values)
    {
        _values = new MultiStringDict(values);
    }

    public virtual MultiString Copy()
    {
        return new(Values);
    }

    private readonly MultiStringDict _values;
    public virtual IDictionary<WritingSystemId, string> Values => _values;

    public string this[WritingSystemId key]
    {
        get => _values[key];
        set => _values[key] = value;
    }
    private class MultiStringDict : Dictionary<WritingSystemId, string>,
#pragma warning disable CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
        IDictionary<WritingSystemId, string>,
#pragma warning restore CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
        IDictionary
    {
        public MultiStringDict()
        {
        }

        public MultiStringDict(IDictionary<WritingSystemId, string> dictionary) : base(dictionary)
        {
        }

        string IDictionary<WritingSystemId, string>.this[WritingSystemId key]
        {
            get => TryGetValue(key, out var value) ? value : string.Empty;
            set => base[key] = value;
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

    void IDictionary.Add(object key, object? value)
    {
        ((IDictionary)_values).Add(key, value);
    }

    void IDictionary.Clear()
    {
        ((IDictionary)_values).Clear();
    }

    bool IDictionary.Contains(object key)
    {
        return ((IDictionary)_values).Contains(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return ((IDictionary)_values).GetEnumerator();
    }

    void IDictionary.Remove(object key)
    {
        ((IDictionary)_values).Remove(key);
    }

    bool IDictionary.IsFixedSize => ((IDictionary)_values).IsFixedSize;

    bool IDictionary.IsReadOnly => ((IDictionary)_values).IsReadOnly;

    object? IDictionary.this[object key]
    {
        get => ((IDictionary)_values)[key];
        set => ((IDictionary)_values)[key] = value;
    }

    ICollection IDictionary.Keys => ((IDictionary)_values).Keys;

    ICollection IDictionary.Values => ((IDictionary)_values).Values;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Values).GetEnumerator();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((IDictionary)_values).CopyTo(array, index);
    }

    int ICollection.Count => ((IDictionary)_values).Count;

    bool ICollection.IsSynchronized => ((IDictionary)_values).IsSynchronized;

    object ICollection.SyncRoot => ((IDictionary)_values).SyncRoot;
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
    public override MultiString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
        if (dict is null) return null;
        var ms = new MultiString();
        foreach (var (key, value) in dict)
        {
            ms.Values[key] = value;
        }

        return ms;
    }

    public override void Write(Utf8JsonWriter writer, MultiString value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Values, options);
    }
}
