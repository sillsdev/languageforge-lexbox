using System.Collections.Frozen;
using MiniLcm.Models;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace FwDataMiniLcmBridge.Api;

public static class RichTextMapping
{
    public static readonly FrozenSet<FwTextPropType> IntProps =
    [
        FwTextPropType.ktptFontSize,
        FwTextPropType.ktptOffset,
        FwTextPropType.ktptFirstIndent,
        FwTextPropType.ktptLeadingIndent,
        FwTextPropType.ktptTrailingIndent,
        FwTextPropType.ktptSpaceBefore,
        FwTextPropType.ktptSpaceAfter,
        FwTextPropType.ktptTabDef,
        FwTextPropType.ktptLineHeight,
        FwTextPropType.ktptRightToLeft,
        FwTextPropType.ktptPadLeading,
        FwTextPropType.ktptPadTrailing,
        FwTextPropType.ktptMarginTop,
        FwTextPropType.ktptPadTop,
        FwTextPropType.ktptPadBottom,
        FwTextPropType.ktptBorderTop,
        FwTextPropType.ktptBorderBottom,
        FwTextPropType.ktptBorderLeading,
        FwTextPropType.ktptBorderTrailing,
        FwTextPropType.ktptBulNumScheme,
        FwTextPropType.ktptBulNumStartAt,
        FwTextPropType.ktptDirectionDepth,
        FwTextPropType.ktptKeepWithNext,
        FwTextPropType.ktptKeepTogether,
        FwTextPropType.ktptHyphenate,
        FwTextPropType.ktptWidowOrphanControl,
        FwTextPropType.ktptMaxLines,
        FwTextPropType.ktptCellBorderWidth,
        FwTextPropType.ktptCellSpacing,
        FwTextPropType.ktptCellPadding,
        FwTextPropType.ktptSetRowDefaults,
        FwTextPropType.ktptRelLineHeight
    ];

    public static readonly FwTextPropType[] StringProps = [
        FwTextPropType.ktptCharStyle,
        FwTextPropType.ktptFontFamily,
        FwTextPropType.ktptNamedStyle,
        FwTextPropType.ktptBulNumTxtBef,
        FwTextPropType.ktptBulNumTxtAft,
        FwTextPropType.ktptFontVariations,
        FwTextPropType.ktptParaStyle,
        FwTextPropType.ktptTabList,
    ];

    public delegate ref T MapPropTypeToRichSpan<T>(RichSpan span);

    public static MapPropTypeToRichSpan<int?> MapToDelegateInt(FwTextPropType type)
    {
        return type switch
        {
            FwTextPropType.ktptFontSize => span => ref span.FontSize,
            FwTextPropType.ktptOffset => span => ref span.Offset,
            FwTextPropType.ktptFirstIndent => span => ref span.FirstIndent,
            FwTextPropType.ktptLeadingIndent => span => ref span.LeadingIndent,
            FwTextPropType.ktptTrailingIndent => span => ref span.TrailingIndent,
            FwTextPropType.ktptSpaceBefore => span => ref span.SpaceBefore,
            FwTextPropType.ktptSpaceAfter => span => ref span.SpaceAfter,
            FwTextPropType.ktptTabDef => span => ref span.TabDef,
            FwTextPropType.ktptLineHeight => span => ref span.LineHeight,
            FwTextPropType.ktptRightToLeft => span => ref span.RightToLeft,
            FwTextPropType.ktptPadLeading => span => ref span.PadLeading,
            FwTextPropType.ktptPadTrailing => span => ref span.PadTrailing,
            FwTextPropType.ktptMarginTop => span => ref span.MarginTop,
            FwTextPropType.ktptPadTop => span => ref span.PadTop,
            FwTextPropType.ktptPadBottom => span => ref span.PadBottom,
            FwTextPropType.ktptBorderTop => span => ref span.BorderTop,
            FwTextPropType.ktptBorderBottom => span => ref span.BorderBottom,
            FwTextPropType.ktptBorderLeading => span => ref span.BorderLeading,
            FwTextPropType.ktptBorderTrailing => span => ref span.BorderTrailing,
            FwTextPropType.ktptBulNumScheme => span => ref span.BulNumScheme,
            FwTextPropType.ktptBulNumStartAt => span => ref span.BulNumStartAt,
            FwTextPropType.ktptDirectionDepth => span => ref span.DirectionDepth,
            FwTextPropType.ktptKeepWithNext => span => ref span.KeepWithNext,
            FwTextPropType.ktptKeepTogether => span => ref span.KeepTogether,
            FwTextPropType.ktptHyphenate => span => ref span.Hyphenate,
            FwTextPropType.ktptWidowOrphanControl => span => ref span.WindowOrphan,
            FwTextPropType.ktptMaxLines => span => ref span.MaxLines,
            FwTextPropType.ktptCellBorderWidth => span => ref span.CellBorderWidth,
            FwTextPropType.ktptCellSpacing => span => ref span.CellSpacing,
            FwTextPropType.ktptCellPadding => span => ref span.CellPadding,
            FwTextPropType.ktptSetRowDefaults => span => ref span.SetRowDefaults,
            FwTextPropType.ktptRelLineHeight => span => ref span.RelLineHeight,
            _ => throw new ArgumentException($"property type {type} is not an integer")
        };
    }
    public static MapPropTypeToRichSpan<string?> MapToDelegateString(FwTextPropType type)
    {
        return type switch
        {
            FwTextPropType.ktptCharStyle => span => ref span.CharStyle,
            FwTextPropType.ktptFontFamily => span => ref span.FontFamily,
            FwTextPropType.ktptNamedStyle => span => ref span.NamedStyle,
            FwTextPropType.ktptBulNumTxtBef => span => ref span.BulNumTxtBef,
            FwTextPropType.ktptBulNumTxtAft => span => ref span.BulNumTxtAft,
            FwTextPropType.ktptFontVariations => span => ref span.FontVariations,
            FwTextPropType.ktptParaStyle => span => ref span.ParaStyle,
            FwTextPropType.ktptTabList => span => ref span.TabList,
            _ => throw new ArgumentException($"property type {type} is not a string")
        };
    }

    public static void WriteToSpan(RichSpan span, ITsTextProps textProps, Func<int?, WritingSystemId?> wsIdLookup)
    {
        for (int i = 0; i < textProps.IntPropCount; i++)
        {
            var value = textProps.GetIntProp(i, out var propTypeInt, out var variation);
            var propType = (FwTextPropType)propTypeInt;
            if (IntProps.Contains(propType))
            {
                MapToDelegateInt(propType).Invoke(span) = value;
            }
        }

        for (int i = 0; i < textProps.StrPropCount; i++)
        {
            var value = textProps.GetStrProp(i, out var propTypeInt);
            var propType = (FwTextPropType)propTypeInt;
            if (StringProps.Contains(propType))
            {
                MapToDelegateString(propType).Invoke(span) = value;
            }
        }

        //todo map other complex props
        span.Ws = wsIdLookup(GetNullableIntProp(textProps, FwTextPropType.ktptWs)) ?? default;
        span.WsBase = wsIdLookup(GetNullableIntProp(textProps, FwTextPropType.ktptBaseWs));
        span.Italic = GetNullableToggleProp(textProps, FwTextPropType.ktptItalic);
        span.Bold = GetNullableToggleProp(textProps, FwTextPropType.ktptBold);
        span.Superscript = GetNullableSuperscriptProp(textProps, FwTextPropType.ktptSuperscript);
        span.Underline = GetNullableUnderlineProp(textProps, FwTextPropType.ktptUnderline);
        span.ForeColor = GetNullableColorProp(textProps, FwTextPropType.ktptForeColor);
        span.BackColor = GetNullableColorProp(textProps, FwTextPropType.ktptBackColor);
        span.UnderColor = GetNullableColorProp(textProps, FwTextPropType.ktptUnderColor);
    }

    public static void WriteToTextProps(RichSpan span,
        ITsPropsBldr builder,
        Func<WritingSystemId, int> wsHandleLookup)
    {
        foreach (var propType in IntProps)
        {
            var prop = MapToDelegateInt(propType).Invoke(span);
            if (prop is not null)
            {
                builder.SetIntPropValues((int)propType, (int)FwTextPropVar.ktpvDefault, prop.Value);
            }
        }

        foreach (var propType in StringProps)
        {
            var prop = MapToDelegateString(propType).Invoke(span);
            if (prop is not null)
            {
                builder.SetStrPropValue((int)propType, prop);
            }
        }

        //todo map other complex props
        builder.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsHandleLookup(span.Ws));
        if (span.WsBase is not null)
            builder.SetIntPropValues((int)FwTextPropType.ktptBaseWs, (int)FwTextPropVar.ktpvDefault, wsHandleLookup(span.WsBase.Value));
    }

    private static int? GetNullableIntProp(ITsTextProps textProps, FwTextPropType type)
    {
        if (textProps.TryGetIntValue(type, out _, out var value))
            return value;
        return null;
    }

    private static RichTextToggle? GetNullableToggleProp(ITsTextProps textProps, FwTextPropType type)
    {
        if (textProps.TryGetIntValue(type, out _, out var value))
            return value switch
            {
                (int)FwTextToggleVal.kttvForceOn => RichTextToggle.On,
                (int)FwTextToggleVal.kttvOff => RichTextToggle.Off,
                (int)FwTextToggleVal.kttvInvert => RichTextToggle.Invert,
                _ => (RichTextToggle)value
            };
        return null;
    }

    private static RichTextSuperscript? GetNullableSuperscriptProp(ITsTextProps textProps, FwTextPropType type)
    {
        if (textProps.TryGetIntValue(type, out _, out var value))
            return value switch
            {
                (int)FwSuperscriptVal.kssvOff => RichTextSuperscript.None,
                (int)FwSuperscriptVal.kssvSuper => RichTextSuperscript.Superscript,
                (int)FwSuperscriptVal.kssvSub => RichTextSuperscript.Subscript,
                _ => (RichTextSuperscript)value
            };
        return null;
    }

    private static RichTextUnderline? GetNullableUnderlineProp(ITsTextProps textProps, FwTextPropType type)
    {
        if (textProps.TryGetIntValue(type, out _, out var value))
            return value switch
            {
                (int)FwUnderlineType.kuntNone => RichTextUnderline.None,
                (int)FwUnderlineType.kuntDotted => RichTextUnderline.Dotted,
                (int)FwUnderlineType.kuntDashed => RichTextUnderline.Dashed,
                (int)FwUnderlineType.kuntSingle => RichTextUnderline.Single,
                (int)FwUnderlineType.kuntDouble => RichTextUnderline.Double,
                (int)FwUnderlineType.kuntSquiggle => RichTextUnderline.Squiggle,
                _ => (RichTextUnderline)value
            };
        return null;
    }

    private static string? GetNullableColorProp(ITsTextProps textProps, FwTextPropType type)
    {
        if (!textProps.TryGetIntValue(type, out _, out var value))
        {
            return null;
        }
        if (value == (int)FwTextColor.kclrTransparent) return "#00000000";
        int blue = (value >> 16) & 0xff;
        int green = (value >> 8) & 0xff;
        int red = value & 0xff;
        return $"#{red:x2}{green:x2}{blue:x2}";
    }

    public static readonly FrozenDictionary<FwTextPropType, string> PropTypeMap = FrozenDictionary
        .ToFrozenDictionary<FwTextPropType, string>([
            new(FwTextPropType.ktptWs, "ws"),
            new(FwTextPropType.ktptBaseWs, "wsBase"),
            new(FwTextPropType.ktptItalic, "italic"),
            new(FwTextPropType.ktptBold, "bold"),
            new(FwTextPropType.ktptSuperscript, "superscript"),
            new(FwTextPropType.ktptUnderline, "underline"),

            new(FwTextPropType.ktptFontSize, "fontSize"),
            new(FwTextPropType.ktptFontVariations, "fontVariations"),
            new(FwTextPropType.ktptFontFamily, "fontFamily"),


            new(FwTextPropType.ktptOffset, "offset"),

            new(FwTextPropType.ktptForeColor, "forecolor"),
            new(FwTextPropType.ktptBackColor, "backcolor"),
            new(FwTextPropType.ktptUnderColor, "undercolor"),
            new(FwTextPropType.ktptAlign, "align"),

            new(FwTextPropType.ktptFirstIndent, "firstIndent"),
            new(FwTextPropType.ktptLeadingIndent, "leadingIndent"),
            new(FwTextPropType.ktptTrailingIndent, "trailingIndent"),
            new(FwTextPropType.ktptSpaceBefore, "spaceBefore"),
            new(FwTextPropType.ktptSpaceAfter, "spaceAfter"),

            new(FwTextPropType.ktptTabDef, "tabDef"),
            new(FwTextPropType.ktptLineHeight, "lineHeight"),
            new(FwTextPropType.ktptParaColor, "paracolor"),
            new(FwTextPropType.ktptSpellCheck, "spellcheck"),
            new(FwTextPropType.ktptRightToLeft, "rightToLeft"),

            new(FwTextPropType.ktptDirectionDepth, "directionDepth"),
            new(FwTextPropType.ktptKeepWithNext, "keepWithNext"),
            new(FwTextPropType.ktptKeepTogether, "keepTogether"),
            new(FwTextPropType.ktptHyphenate, "hyphenate"),
            new(FwTextPropType.ktptMaxLines, "maxLines"),
            new(FwTextPropType.ktptCellBorderWidth, "cellBorderWidth"),
            new(FwTextPropType.ktptCellSpacing, "cellSpacing"),
            new(FwTextPropType.ktptCellPadding, "cellPadding"),
            new(FwTextPropType.ktptEditable, "editable"),
            new(FwTextPropType.ktptSetRowDefaults, "setRowDefaults"),
            new(FwTextPropType.ktptRelLineHeight, "relLineHeight"),
            new(FwTextPropType.ktptWidowOrphanControl, "widowOrphan"),

            new(FwTextPropType.ktptMarginBottom, "marginBottom"),
            new(FwTextPropType.ktptMarginLeading, "marginLeading"),
            new(FwTextPropType.ktptMarginTop, "marginTop"),
            new(FwTextPropType.ktptMarginTrailing, "marginTrailing"),
            new(FwTextPropType.ktptMswMarginTop, "mswMarginTop"),
            new(FwTextPropType.ktptPadBottom, "padBottom"),
            new(FwTextPropType.ktptPadLeading, "padLeading"),
            new(FwTextPropType.ktptPadTop, "padTop"),
            new(FwTextPropType.ktptPadTrailing, "padTrailing"),


            new(FwTextPropType.ktptSpaceAfter, "spaceAfter"),
            new(FwTextPropType.ktptSpaceBefore, "spaceBefore"),
            new(FwTextPropType.ktptTabDef, "tabDef"),
            new(FwTextPropType.ktptLineHeight, "lineHeight"),
            new(FwTextPropType.ktptParaColor, "paraColor"),
            new(FwTextPropType.ktptParaStyle, "paraStyle"),
            new(FwTextPropType.ktptBorderTop, "borderTop"),
            new(FwTextPropType.ktptBorderBottom, "borderBottom"),
            new(FwTextPropType.ktptBorderLeading, "borderLeading"),
            new(FwTextPropType.ktptBorderTrailing, "borderTrailing"),
            new(FwTextPropType.ktptBorderColor, "borderColor"),
            new(FwTextPropType.ktptBulNumScheme, "bulNumScheme"),
            new(FwTextPropType.ktptBulNumStartAt, "bulNumStartAt"),
            new(FwTextPropType.ktptBulNumTxtBef, "bulNumTxtBef"),
            new(FwTextPropType.ktptBulNumTxtAft, "bulNumTxtAft"),
            new(FwTextPropType.ktptBulNumFontInfo, "bulNumFontInfo"),
            new(FwTextPropType.ktptWsStyle, "wsStyle"),
            new(FwTextPropType.ktptNamedStyle, "namedStyle"),
            new(FwTextPropType.ktptTableRule, "tableRule"),
            new(FwTextPropType.ktptFieldName, "fieldName"),

        ]);
}
