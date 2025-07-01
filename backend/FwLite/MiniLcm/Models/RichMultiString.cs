using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonConverter(typeof(RichMultiStringConverter))]
public class RichMultiString(IDictionary<WritingSystemId, RichString> dictionary) : IDictionary, IEnumerable<KeyValuePair<WritingSystemId, RichString>>
{
    protected readonly IDictionary<WritingSystemId, RichString> dictionary = dictionary;

    public RichMultiString() : this(new Dictionary<WritingSystemId, RichString>())
    {
    }

    public RichMultiString(int capacity) : this(new Dictionary<WritingSystemId, RichString>(capacity))
    {
    }

    public override string ToString()
    {
        if (Count == 0) return "{}";
        return string.Join(", ", dictionary.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    public RichMultiString Copy()
    {
        var newRichString = new RichMultiString(Count);
        foreach (var keyValuePair in dictionary)
        {
            if (keyValuePair.Value is null) continue;
            newRichString.Add(keyValuePair.Key, keyValuePair.Value.Copy());
        }

        return newRichString;
    }

    void IDictionary.Add(object key, object? value)
    {
        var valStr = value as RichString ??
                     throw new ArgumentException($"unable to convert value {value?.GetType().Name ?? "null"} to RichString",
                         nameof(value));
        Add(WritingSystemId.FromUnknown(key), valStr);
    }

    public void Add(WritingSystemId key, RichString value)
    {
        value.EnsureWs(key);
        dictionary.Add(key, value);
    }

    public IEnumerator<KeyValuePair<WritingSystemId, RichString>> GetEnumerator()
    {
        return dictionary.GetEnumerator();
    }

    public void Add(KeyValuePair<WritingSystemId, RichString> item)
    {
        dictionary.Add(item);
    }

    public void Clear()
    {
        dictionary.Clear();
    }

    public bool Contains(KeyValuePair<WritingSystemId, RichString> item)
    {
        return dictionary.Contains(item);
    }

    public bool Remove(KeyValuePair<WritingSystemId, RichString> item)
    {
        return dictionary.Remove(item);
    }

    public int Count => dictionary.Count;

    public bool IsReadOnly => dictionary.IsReadOnly;

    public bool ContainsKey(WritingSystemId key)
    {
        return dictionary.ContainsKey(key);
    }

    public bool Remove(WritingSystemId key)
    {
        return dictionary.Remove(key);
    }

    public bool TryGetValue(WritingSystemId key, [MaybeNullWhen(false)] out RichString value)
    {
        return dictionary.TryGetValue(key, out value);
    }

    public RichString this[WritingSystemId key]
    {
        get => dictionary.TryGetValue(key, out var value) ? value : new RichString([]);
        set
        {
            value.EnsureWs(key);
            dictionary[key] = value;
        }
    }

    public ICollection<WritingSystemId> Keys => dictionary.Keys;

    public ICollection<RichString> Values => dictionary.Values;
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    void ICollection.CopyTo(Array array, int index)
    {
        ((IDictionary)dictionary).CopyTo(array, index);
    }

    bool ICollection.IsSynchronized => ((IDictionary)dictionary).IsSynchronized;

    object ICollection.SyncRoot => ((IDictionary)dictionary).SyncRoot;

    bool IDictionary.Contains(object key)
    {
        return ((IDictionary)dictionary).Contains(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return ((IDictionary)dictionary).GetEnumerator();
    }

    void IDictionary.Remove(object key)
    {
        ((IDictionary)dictionary).Remove(key);
    }

    bool IDictionary.IsFixedSize => ((IDictionary)dictionary).IsFixedSize;

    object? IDictionary.this[object key]
    {
        get => this[WritingSystemId.FromUnknown(key)];
        set
        {
            // value will be null if an empty string was deserialized as a RichString (e.g. from a JsonPatch operation).
            // Usually that's what we want, because when deserializing a whole RichMultiString it will result in the key being dropped.
            // So we mimic that behaviour.
            var wsId = WritingSystemId.FromUnknown(key);
            if (value is null)
            {
                Remove(wsId);
            }
            else
            {
                var valStr = value as RichString ??
                             throw new ArgumentException("unable to convert value to string", nameof(value));
                this[wsId] = valStr;
            }
        }
    }

    ICollection IDictionary.Keys => (ICollection) dictionary.Keys;

    ICollection IDictionary.Values => (ICollection) dictionary.Values;
}

public class RichMultiStringConverter : JsonConverter<RichMultiString>
{
    public override RichMultiString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, RichString?>>(ref reader, options);
        if (dict is null) return null;
        var ms = new RichMultiString();
        foreach (var (key, value) in dict)
        {
            if (value?.Spans is [] or null) continue;
            value.EnsureWs(key);
            ms[key] = value;
        }

        return ms;
    }

    public override void Write(Utf8JsonWriter writer, RichMultiString value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, typeof(IDictionary), options);
    }
}
