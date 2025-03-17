using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using HtmlAgilityPack;

namespace MiniLcm.Models;

//todo, migrate to the new format, use RichString instead of string
[JsonConverter(typeof(RichMultiStringConverter))]
public class RichMultiString : Dictionary<WritingSystemId, string>, IDictionary
{
    public static bool IsValidRichString(string value)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(value);
        if (doc.ParseErrors.Any()) return false;
        if (doc.DocumentNode.ChildNodes.Count == 0) return false;
        foreach (var childNode in doc.DocumentNode.ChildNodes)
        {
            if (childNode.NodeType != HtmlNodeType.Element) return false;
            foreach (var childNodeChildNode in childNode.ChildNodes)
            {
                if (childNodeChildNode.NodeType != HtmlNodeType.Text) return false;
            }
        }

        return true;
    }

    public RichMultiString(IDictionary<WritingSystemId, string> dictionary) : base(dictionary)
    {
    }

    public RichMultiString()
    {
    }

    public RichMultiString(int capacity) : base(capacity)
    {
    }

    public new string this[WritingSystemId key]
    {
        get => TryGetValue(key, out var value) ? value : string.Empty;
        set
        {
            // disabled so we can merge this PR without figuring out how to migrate data to the new format
//             if (!IsValidRichString(value))
//             {
//                 throw new ArgumentException($"""
//                                              Invalid rich string "{value}"
//                                              """);
//             }
            base[key] = value;
        }
    }

    public override string ToString()
    {
        if (Count == 0) return "{}";
        return string.Join(", ", this.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    public RichMultiString Copy()
    {
        return new RichMultiString(this);
    }

    void IDictionary.Add(object key, object? value)
    {
        var valStr = value as string ??
                     throw new ArgumentException($"unable to convert value {value?.GetType().Name ?? "null"} to string",
                         nameof(value));
        // if (!IsValidRichString(valStr))
        // {
        //     throw new ArgumentException($"Invalid rich string {valStr}");
        // }
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
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
        if (dict is null) return null;
        var ms = new RichMultiString();
        foreach (var (key, value) in dict)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            ms[key] = value;
        }

        return ms;
    }

    public override void Write(Utf8JsonWriter writer, RichMultiString value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, typeof(Dictionary<WritingSystemId, string>), options);
    }
}
