using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using MiniLcm.Models;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

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
        FwTextPropType.ktptRelLineHeight,
        FwTextPropType.ktptTableRule,
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
        FwTextPropType.ktptCustomBullet,
        FwTextPropType.ktptBulNumFontInfo,
        FwTextPropType.ktptWsStyle,
        FwTextPropType.ktptFieldName,
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
            FwTextPropType.ktptWidowOrphanControl => span => ref span.WidowOrphan,
            FwTextPropType.ktptMaxLines => span => ref span.MaxLines,
            FwTextPropType.ktptCellBorderWidth => span => ref span.CellBorderWidth,
            FwTextPropType.ktptCellSpacing => span => ref span.CellSpacing,
            FwTextPropType.ktptCellPadding => span => ref span.CellPadding,
            FwTextPropType.ktptSetRowDefaults => span => ref span.SetRowDefaults,
            FwTextPropType.ktptRelLineHeight => span => ref span.RelLineHeight,
            FwTextPropType.ktptTableRule => span => ref span.TableRule,
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
            FwTextPropType.ktptCustomBullet => span => ref span.CustomBullet,
            FwTextPropType.ktptBulNumFontInfo => span => ref span.BulNumFontInfo,
            FwTextPropType.ktptWsStyle => span => ref span.WsStyle,
            FwTextPropType.ktptFieldName => span => ref span.FieldName,
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

        span.Ws = wsIdLookup(GetNullableIntProp(textProps, FwTextPropType.ktptWs));
        span.WsBase = wsIdLookup(GetNullableIntProp(textProps, FwTextPropType.ktptBaseWs));
        span.Italic = GetNullableToggleProp(textProps, FwTextPropType.ktptItalic);
        span.Bold = GetNullableToggleProp(textProps, FwTextPropType.ktptBold);
        span.Superscript = GetNullableSuperscriptProp(textProps);
        span.Underline = GetNullableUnderlineProp(textProps);
        span.ForeColor = GetNullableColorProp(textProps, FwTextPropType.ktptForeColor);
        span.BackColor = GetNullableColorProp(textProps, FwTextPropType.ktptBackColor);
        span.UnderColor = GetNullableColorProp(textProps, FwTextPropType.ktptUnderColor);
        span.ParaColor = GetNullableColorProp(textProps, FwTextPropType.ktptParaColor);
        span.Align = GetNullableRichTextAlign(textProps);
        span.SpellCheck = GetNullableRichTextSpellCheck(textProps, FwTextPropType.ktptSpellCheck);
        span.Tags = GetNullableRichTextTags(textProps);
        span.ObjData = GetRichObjectData(textProps);
        span.BorderColor = GetNullableColorProp(textProps, FwTextPropType.ktptBorderColor);
        span.Editable = GetNullableRichTextEditable(textProps);
        span.FontSizeUnit = GetNullableSizeUnit(textProps, FwTextPropType.ktptFontSize);
        span.OffsetUnit = GetNullableSizeUnit(textProps, FwTextPropType.ktptOffset);
        span.LineHeightUnit = GetNullableSizeUnit(textProps, FwTextPropType.ktptLineHeight);
    }

    private static RichTextSizeUnit? GetNullableSizeUnit(ITsTextProps textProps, FwTextPropType type)
    {
        if (textProps.TryGetIntValue(type, out var variation, out var value))
        {
            return variation switch
            {
                FwTextPropVar.ktpvDefault => null,
                FwTextPropVar.ktpvMilliPoint => RichTextSizeUnit.MilliPoint,
                FwTextPropVar.ktpvRelative => RichTextSizeUnit.Relative,
                _ => (RichTextSizeUnit?)variation
            };
        }
        return null;
    }

    private static int ReverseSizeUnit(RichTextSizeUnit? unit)
    {
        return unit switch
        {
            null => (int)FwTextPropVar.ktpvDefault,
            RichTextSizeUnit.Relative => (int)FwTextPropVar.ktpvRelative,
            _ => (int)FwTextPropVar.ktpvMilliPoint,
        };
    }

    private static RichTextEditable? GetNullableRichTextEditable(ITsTextProps textProps)
    {
        if (textProps.TryGetIntValue(FwTextPropType.ktptEditable, out _, out var value))
        {
            return value switch
            {
                (int)TptEditable.ktptNotEditable => RichTextEditable.NotEditable,
                (int)TptEditable.ktptIsEditable => RichTextEditable.IsEditable,
                (int)TptEditable.ktptSemiEditable => RichTextEditable.SemiEditable,
                _ => (RichTextEditable?)value
            };
        }
        return null;
    }

    private static int? ReverseEditable(RichTextEditable? editable)
    {
        return editable switch
        {
            null => null,
            RichTextEditable.NotEditable => (int)TptEditable.ktptNotEditable,
            RichTextEditable.IsEditable => (int)TptEditable.ktptIsEditable,
            RichTextEditable.SemiEditable => (int)TptEditable.ktptSemiEditable,
            _ => (int?)editable
        };
    }

    private static Guid[]? GetNullableRichTextTags(ITsTextProps textProps)
    {
        if (textProps.TryGetStringValue(FwTextPropType.ktptTags, out var value))
        {
            if (string.IsNullOrEmpty(value))
                return null;
            const int guidLength = 16;
            var bytes = Encoding.Unicode.GetBytes(value).AsSpan();
            Guid[] guids = new Guid[bytes.Length / guidLength];
            for (int i = 0; i < guids.Length; i++)
            {
                guids[i] = new Guid(bytes.Slice(i * guidLength, guidLength));
            }
            return guids;
        }

        return null;
    }

    private static string ReverseTags(Guid[] tags)
    {
        return string.Join("", tags.Select(MiscUtils.GetObjDataFromGuid));
    }

    private static RichTextSpellingMode? GetNullableRichTextSpellCheck(ITsTextProps textProps, FwTextPropType type)
    {
        if (textProps.TryGetIntValue(type, out _, out var value))
            return value switch
            {
                (int)SpellingModes.ksmNormalCheck => RichTextSpellingMode.Normal,
                (int)SpellingModes.ksmDoNotCheck => RichTextSpellingMode.DoNotCheck,
                (int)SpellingModes.ksmForceCheck => RichTextSpellingMode.ForceCheck,
                _ => (RichTextSpellingMode)value
            };
        return null;
    }

    private static int? ReverseSpellCheck(RichTextSpellingMode? spellCheck)
    {
        return spellCheck switch
        {
            null => null,
            RichTextSpellingMode.Normal => (int)SpellingModes.ksmNormalCheck,
            RichTextSpellingMode.DoNotCheck => (int)SpellingModes.ksmDoNotCheck,
            RichTextSpellingMode.ForceCheck => (int)SpellingModes.ksmForceCheck,
            _ => (int?)spellCheck
        };
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
                //todo set default based on propType
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

        if (span.Ws is not null)
            SetInt(builder, FwTextPropType.ktptWs, wsHandleLookup(span.Ws.Value));
        if (span.WsBase is not null)
            SetInt(builder, FwTextPropType.ktptBaseWs, wsHandleLookup(span.WsBase.Value));
        SetInt(builder, FwTextPropType.ktptItalic, ReverseMapToggle(span.Italic));
        SetInt(builder, FwTextPropType.ktptBold, ReverseMapToggle(span.Bold));
        SetInt(builder, FwTextPropType.ktptSuperscript, ReverseSuperscript(span.Superscript));
        SetInt(builder, FwTextPropType.ktptUnderline, ReverseUnderline(span.Underline));
        SetInt(builder, FwTextPropType.ktptForeColor, ReverseColor(span.ForeColor));
        SetInt(builder, FwTextPropType.ktptBackColor, ReverseColor(span.BackColor));
        SetInt(builder, FwTextPropType.ktptUnderColor, ReverseColor(span.UnderColor));
        SetInt(builder, FwTextPropType.ktptParaColor, ReverseColor(span.ParaColor));
        SetInt(builder, FwTextPropType.ktptBorderColor, ReverseColor(span.BorderColor));
        SetInt(builder, FwTextPropType.ktptAlign, ReverseAlign(span.Align));
        SetInt(builder, FwTextPropType.ktptSpellCheck, ReverseSpellCheck(span.SpellCheck));


        SetInt(builder, FwTextPropType.ktptFontSize, span.FontSize, ReverseSizeUnit(span.FontSizeUnit));
        SetInt(builder, FwTextPropType.ktptOffset, span.Offset, ReverseSizeUnit(span.OffsetUnit));
        SetInt(builder, FwTextPropType.ktptLineHeight, span.LineHeight, ReverseSizeUnit(span.LineHeightUnit));
        SetInt(builder, FwTextPropType.ktptEditable, ReverseEditable(span.Editable));

        if (span.Tags is not null)
        {
            builder.SetStrPropValue((int)FwTextPropType.ktptTags, ReverseTags(span.Tags));
        }

        if (span.ObjData is not null)
        {
            builder.SetStrPropValue((int)FwTextPropType.ktptObjData, span.ObjData.RawString);
        }
    }

    private static void SetInt(ITsPropsBldr builder, FwTextPropType type, int? value, int variation = 0)
    {
        if (value is not null)
            builder.SetIntPropValues((int)type, variation, value.Value);
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

    private static int? ReverseMapToggle(RichTextToggle? toggle)
    {
        return toggle switch
        {
            null => null,
            RichTextToggle.On => (int)FwTextToggleVal.kttvForceOn,
            RichTextToggle.Off => (int)FwTextToggleVal.kttvOff,
            RichTextToggle.Invert => (int)FwTextToggleVal.kttvInvert,
            _ => (int?)toggle
        };
    }

    private static RichTextSuperscript? GetNullableSuperscriptProp(ITsTextProps textProps)
    {
        if (textProps.TryGetIntValue(FwTextPropType.ktptSuperscript, out _, out var value))
            return value switch
            {
                (int)FwSuperscriptVal.kssvOff => RichTextSuperscript.None,
                (int)FwSuperscriptVal.kssvSuper => RichTextSuperscript.Superscript,
                (int)FwSuperscriptVal.kssvSub => RichTextSuperscript.Subscript,
                _ => (RichTextSuperscript)value
            };
        return null;
    }

    private static int? ReverseSuperscript(RichTextSuperscript? superscript)
    {
        return superscript switch
        {
            null => null,
            RichTextSuperscript.None => (int)FwSuperscriptVal.kssvOff,
            RichTextSuperscript.Superscript => (int)FwSuperscriptVal.kssvSuper,
            RichTextSuperscript.Subscript => (int)FwSuperscriptVal.kssvSub,
            _ => (int?)superscript
        };
    }

    private static RichTextUnderline? GetNullableUnderlineProp(ITsTextProps textProps)
    {
        if (textProps.TryGetIntValue(FwTextPropType.ktptUnderline, out _, out var value))
            return value switch
            {
                (int)FwUnderlineType.kuntNone => RichTextUnderline.None,
                (int)FwUnderlineType.kuntDotted => RichTextUnderline.Dotted,
                (int)FwUnderlineType.kuntDashed => RichTextUnderline.Dashed,
                (int)FwUnderlineType.kuntSingle => RichTextUnderline.Single,
                (int)FwUnderlineType.kuntDouble => RichTextUnderline.Double,
                (int)FwUnderlineType.kuntStrikethrough => RichTextUnderline.Strikethrough,
                (int)FwUnderlineType.kuntSquiggle => RichTextUnderline.Squiggle,
                _ => (RichTextUnderline)value
            };
        return null;
    }

    private static int? ReverseUnderline(RichTextUnderline? underline)
    {
        return underline switch
        {
            null => null,
            RichTextUnderline.None => (int)FwUnderlineType.kuntNone,
            RichTextUnderline.Single => (int)FwUnderlineType.kuntSingle,
            RichTextUnderline.Double => (int)FwUnderlineType.kuntDouble,
            RichTextUnderline.Dotted => (int)FwUnderlineType.kuntDotted,
            RichTextUnderline.Dashed => (int)FwUnderlineType.kuntDashed,
            RichTextUnderline.Strikethrough => (int)FwUnderlineType.kuntStrikethrough,
            RichTextUnderline.Squiggle => (int)FwUnderlineType.kuntSquiggle,
            _ => (int?)underline
        };
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

    private static int? ReverseColor(string? rgb)
    {
        if (string.IsNullOrEmpty(rgb))
            return null;
        if (rgb == "#00000000")
            return (int)FwTextColor.kclrTransparent;
        return (int)ColorUtil.ConvertRGBtoBGR(uint.Parse(rgb.AsSpan()[1..], NumberStyles.HexNumber));
    }

    private static RichTextAlign? GetNullableRichTextAlign(ITsTextProps textProps)
    {
        if (textProps.TryGetIntValue(FwTextPropType.ktptAlign, out _, out var value))
        {
            return value switch
            {
                (int)FwTextAlign.ktalLeading => RichTextAlign.Leading,
                (int)FwTextAlign.ktalLeft => RichTextAlign.Left,
                (int)FwTextAlign.ktalCenter => RichTextAlign.Center,
                (int)FwTextAlign.ktalRight => RichTextAlign.Right,
                (int)FwTextAlign.ktalTrailing => RichTextAlign.Trailing,
                (int)FwTextAlign.ktalJustify => RichTextAlign.Justify,
                _ => (RichTextAlign)value
            };
        }

        return null;
    }

    private static int? ReverseAlign(RichTextAlign? align)
    {
        return align switch
        {
            null => null,
            RichTextAlign.Leading => (int)FwTextAlign.ktalLeading,
            RichTextAlign.Left => (int)FwTextAlign.ktalLeft,
            RichTextAlign.Center => (int)FwTextAlign.ktalCenter,
            RichTextAlign.Right => (int)FwTextAlign.ktalRight,
            RichTextAlign.Trailing => (int)FwTextAlign.ktalTrailing,
            RichTextAlign.Justify => (int)FwTextAlign.ktalJustify,
            _ => (int?)align
        };
    }

    private static RichTextObjectData? GetRichObjectData(ITsTextProps textProps)
    {
        var rawDataString = textProps.GetStrPropValue((int)FwTextPropType.ktptObjData);
        if (string.IsNullOrEmpty(rawDataString))
            return null;
        return RichTextObjectData.FromString(rawDataString);
    }
}
