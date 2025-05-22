using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonConverter(typeof(RichMultiStringConverter))]
public class RichMultiString : Dictionary<WritingSystemId, RichString>, IDictionary
{
    public RichMultiString(IDictionary<WritingSystemId, RichString> dictionary) : base(dictionary)
    {
    }

    public RichMultiString()
    {
    }

    public RichMultiString(int capacity) : base(capacity)
    {
    }

    public override string ToString()
    {
        if (Count == 0) return "{}";
        return string.Join(", ", this.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    public RichMultiString Copy()
    {
        var newRichString = new RichMultiString(Count);
        foreach (var keyValuePair in this)
        {
            if (keyValuePair.Value is null) continue;
            newRichString.Add(keyValuePair.Key, keyValuePair.Value.Copy());
        }

        return newRichString;
    }

    void IDictionary.Add(object key, object? value)
    {
        var valStr = value as RichString ??
                     throw new ArgumentException($"unable to convert value {value?.GetType().Name ?? "null"} to string",
                         nameof(value));
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
            ms[key] = value;
        }

        return ms;
    }

    public override void Write(Utf8JsonWriter writer, RichMultiString value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, typeof(Dictionary<WritingSystemId, RichString>), options);
    }
}
