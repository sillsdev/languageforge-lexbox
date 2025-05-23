using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonConverter(typeof(RichStringConverter))]
public class RichString(List<RichSpan> spans) : IEquatable<RichString>
{
    public RichString(string text) : this([new RichSpan { Text = text }])
    {
    }

    public List<RichSpan> Spans { get; set; } = spans;
    [JsonIgnore]
    public bool IsEmpty => Spans.Count == 0 || Spans.All(s => string.IsNullOrEmpty(s.Text));

    internal class RichStringConverter: JsonConverter<RichString>
    {
        //helper class which doesn't have a converter
        private class RichStringPrimitive(List<RichSpan> Spans) : RichString(Spans);
        public override RichString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var text = reader.GetString();
                if (string.IsNullOrWhiteSpace(text)) return null;
                return new RichString(text);
            }
            var model = JsonSerializer.Deserialize<RichStringPrimitive>(ref reader, options);
            return model?.Spans is null ? null : new RichString(model.Spans);
        }

        public override void Write(Utf8JsonWriter writer, RichString value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, new RichStringPrimitive(value.Spans), options);
        }
    }

    public RichString Copy()
    {
        var newSpans = Spans.Select(span => span with
        {
            ObjData = span.ObjData is null ? null : span.ObjData with {},
            Tags = span.Tags?.ToArray()
        }).ToList();
        return new RichString(newSpans);
    }

    public bool Equals(RichString? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Spans.SequenceEqual(other.Spans);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((RichString)obj);
    }
}


public record RichSpan
{
    public required string Text { get; init; }

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WritingSystemId? Ws;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WritingSystemId? WsBase;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextToggle? Italic;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextToggle? Bold;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextAlign? Align;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextSuperscript? Superscript;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextUnderline? Underline;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FontFamily;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FontSize;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextSizeUnit? FontSizeUnit;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FontVariations;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Offset;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextSizeUnit? OffsetUnit;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ForeColor;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BackColor;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UnderColor;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FirstIndent;
    //aka MarginLeading
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LeadingIndent;
    //aka MarginTrailing
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TrailingIndent;
    //aka MswMarginTop
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SpaceBefore;
    //aka MarginBottom
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SpaceAfter;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MarginTop;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TabDef;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LineHeight;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextSizeUnit? LineHeightUnit;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParaColor;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextSpellingMode? SpellCheck;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RightToLeft;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DirectionDepth;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? KeepWithNext;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? KeepTogether;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Hyphenate;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxLines;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CellBorderWidth;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CellSpacing;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CellPadding;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextEditable? Editable;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SetRowDefaults;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RelLineHeight;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WidowOrphan;


    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PadTop;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PadBottom;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PadLeading;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PadTrailing;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParaStyle;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CharStyle;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NamedStyle;

    //big blob of style data encoded as a string, no longer used
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WsStyle;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BorderTop;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BorderBottom;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BorderLeading;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BorderTrailing;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BorderColor;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BulNumScheme;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BulNumStartAt;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BulNumTxtBef;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BulNumTxtAft;
    //similar to WsStyle, it's a blob of style data, we just want to pass is through
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BulNumFontInfo;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomBullet;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TabList;

    //not used anymore? mapping is simple enough might as well.
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TableRule;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FieldName;

    //can be a number of different things depending on the string
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RichTextObjectData? ObjData;
    //note an empty array will be converted to null on round trip to FW
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid[]? Tags;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextToggle
{
    Off = 0,
    On = 1,
    Invert = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextAlign
{
    Leading = 0,
    Left = 1,
    Center = 2,
    Right = 3,
    Trailing = 4,
    Justify = 5,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextSizeUnit
{
    MilliPoint = 0,
    Relative = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextLineHeightType
{
    Exact = 0,
    AtLeast = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextSpellingMode
{
    Normal = 0,
    DoNotCheck = 1,
    ForceCheck = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextEditable
{
    NotEditable = 0,
    IsEditable = 1,
    SemiEditable = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextSuperscript
{
    None = 0,
    Superscript = 1,
    Subscript = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextUnderline
{
    None = 0,
    Dotted = 1,
    Dashed = 2,
    Strikethrough = 3,
    Single = 4,
    Double = 5,
    Squiggle = 6
}

public record RichTextObjectData
{
    public required string Value { get; init; }
    public required RichTextObjectDataType Type { get; init; }

    [JsonIgnore]
    public bool IsGuidType =>
        Type is RichTextObjectDataType.NameGuid or RichTextObjectDataType.OwnNameGuid
            or RichTextObjectDataType.GuidMoveableObjDisp or RichTextObjectDataType.ContextString;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RichTextObjectDataType
{
    //note these numbers must match the enum values in FwObjDataTypes
    Unknown = 0,
    PictureEven = 1,
    PictureOdd = 2,
    NameGuid = 3,
    ExternalPathName = 4,
    OwnNameGuid = 5,
    EmbeddedObjectData = 6,
    ContextString = 7,
    GuidMoveableObjDisp = 8,
}
