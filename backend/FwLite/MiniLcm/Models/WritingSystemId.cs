using System.Text.Json;
using System.Text.Json.Serialization;
using SIL.WritingSystems;

namespace MiniLcm.Models;

public class WritingSystemIdJsonConverter : JsonConverter<WritingSystemId>
{
    public override WritingSystemId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return WritingSystemId.Parse(reader.GetString() ??
                                     throw new NullReferenceException("can't convert null to a writing system"), null);
    }

    public override void Write(Utf8JsonWriter writer, WritingSystemId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override WritingSystemId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => WritingSystemId.Parse(reader.GetString() ?? throw new NullReferenceException("can't convert null to a writing system"), null);

    public override void WriteAsPropertyName(Utf8JsonWriter writer, WritingSystemId value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}

[JsonConverter(typeof(WritingSystemIdJsonConverter))]
public readonly record struct WritingSystemId: ISpanFormattable, ISpanParsable<WritingSystemId>
{
    public string Code { get; init; }
    public bool IsAudio { get; } = false;

    public static readonly WritingSystemId Default = "default";

    public WritingSystemId(string code)
    {
        //__key is used by the LfClassicMiniLcmApi to smuggle non guid ids with possibilitie lists
        if (code == "default" || code == "__key")
        {
            Code = code;
        }
        else if (IetfLanguageTag.TryGetSubtags(code,
                     out LanguageSubtag subtag,
                     out ScriptSubtag scriptSubtag,
                     out RegionSubtag regionSubtag,
                     out IEnumerable<VariantSubtag> variantSubtags))
        {
            Code = code;
            VariantSubtag audioVariant = WellKnownSubtags.AudioPrivateUse;
            IsAudio = scriptSubtag.Code.Equals(WellKnownSubtags.AudioScript, StringComparison.OrdinalIgnoreCase) &&
                      variantSubtags.Any(v => v == audioVariant);
        }
        else
        {
            throw new ArgumentException($"Invalid writing system ID '{code}'", nameof(code));
        }
    }

    public static implicit operator string(WritingSystemId ws) => ws.Code;
    public static implicit operator WritingSystemId(string code) => new(code);
    public static implicit operator WritingSystemId(ReadOnlySpan<char> code) => new(new string(code));

    public override string ToString()
    {
        return Code;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return Code;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        Code.AsSpan().CopyTo(destination);
        charsWritten = Code.Length;
        return true;
    }

    public static WritingSystemId Parse(string s, IFormatProvider? provider)
    {
        return s;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out WritingSystemId result)
    {
        result = default;
        if (s is null) return false;
        result = s;
        return true;
    }

    public static WritingSystemId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return s;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out WritingSystemId result)
    {
        result = s;
        return true;
    }
}
