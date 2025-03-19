namespace MiniLcm.Models;

public class RichString
{
    public required List<RichSpan> Spans { get; set; }
}

public class RichSpan
{
    public required string Text { get; init; }

    public WritingSystemId? Ws;
    public WritingSystemId? WsBase;

    public RichTextToggle? Italic;
    public RichTextToggle? Bold;
    public RichTextAlign? Align;

    public RichTextSuperscript? Superscript;
    public RichTextUnderline? Underline;

    public string? FontFamily;
    public int? FontSize;
    public RichTextSizeUnit? FontSizeUnit;
    public string? FontVariations;

    public int? Offset;
    public RichTextSizeUnit? OffsetUnit;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    public string? ForeColor;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    public string? BackColor;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    public string? UnderColor;

    public int? FirstIndent;
    //aka MarginLeading
    public int? LeadingIndent;
    //aka MarginTrailing
    public int? TrailingIndent;
    //aka MswMarginTop
    public int? SpaceBefore;
    //aka MarginBottom
    public int? SpaceAfter;
    public int? MarginTop;

    public int? TabDef;
    public int? LineHeight;
    public RichTextSizeUnit? LineHeightUnit;

    /// <summary>
    /// RGB, like #ffffff, or #00000000 for transparent
    /// </summary>
    public string? ParaColor;

    public RichTextSpellingMode? SpellCheck;
    public int? RightToLeft;
    public int? DirectionDepth;
    public int? KeepWithNext;
    public int? KeepTogether;
    public int? Hyphenate;
    public int? MaxLines;

    public int? CellBorderWidth;
    public int? CellSpacing;
    public int? CellPadding;

    public RichTextEditable? Editable;
    public int? SetRowDefaults;
    public int? RelLineHeight;

    public int? WidowOrphan;


    public int? PadTop;
    public int? PadBottom;
    public int? PadLeading;
    public int? PadTrailing;

    public string? ParaStyle;
    public string? CharStyle;
    public string? NamedStyle;

    //big blob of style data encoded as a string, no longer used
    public string? WsStyle;

    public int? BorderTop;
    public int? BorderBottom;
    public int? BorderLeading;
    public int? BorderTrailing;
    public string? BorderColor;

    public int? BulNumScheme;
    public int? BulNumStartAt;
    public string? BulNumTxtBef;
    public string? BulNumTxtAft;
    //similar to WsStyle, it's a blob of style data, we just want to pass is through
    public string? BulNumFontInfo;
    public string? CustomBullet;

    public string? TabList;

    //not used anymore? mapping is simple enough might as well.
    public int? TableRule;
    public string? FieldName;

    //can be a number of different things depending on the string
    public RichTextObjectData? ObjData;
    //note an empty array will be converted to null on round trip to FW
    public Guid[]? Tags;
}

public enum RichTextToggle
{
    Off = 0,
    On = 1,
    Invert = 2
}

public enum RichTextAlign
{
    Leading = 0,
    Left = 1,
    Center = 2,
    Right = 3,
    Trailing = 4,
    Justify = 5,
}

public enum RichTextSizeUnit
{
    MilliPoint = 0,
    Relative = 1,
}

public enum RichTextLineHeightType
{
    Exact = 0,
    AtLeast = 1,
}

public enum RichTextSpellingMode
{
    Normal = 0,
    DoNotCheck = 1,
    ForceCheck = 2,
}

public enum RichTextEditable
{
    NotEditable = 0,
    IsEditable = 1,
    SemiEditable = 2,
}

public enum RichTextSuperscript
{
    None = 0,
    Superscript = 1,
    Subscript = 2,
}

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

public class RichTextObjectData
{
    public required string Value { get; init; }
    public required RichTextObjectDataType Type { get; init; }

    public bool IsGuidType =>
        Type is RichTextObjectDataType.NameGuid or RichTextObjectDataType.OwnNameGuid
            or RichTextObjectDataType.GuidMoveableObjDisp or RichTextObjectDataType.ContextString;
}

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
