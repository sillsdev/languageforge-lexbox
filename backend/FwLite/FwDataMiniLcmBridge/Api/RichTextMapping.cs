using System.Collections.Frozen;
using SIL.LCModel.Core.KernelInterfaces;

namespace FwDataMiniLcmBridge.Api;

public static class RichTextMapping
{
    public static readonly FrozenDictionary<FwTextPropType, string> PropTypeMap = FrozenDictionary
        .ToFrozenDictionary<FwTextPropType, string>([
            new(FwTextPropType.ktptWs, "ws"),
            new(FwTextPropType.ktptBaseWs, "wsBase"),
            new(FwTextPropType.ktptItalic, "italic"),
            new(FwTextPropType.ktptBold, "bold"),
            new(FwTextPropType.ktptSuperscript, "superscript"),
            new(FwTextPropType.ktptUnderline, "underline"),
            new(FwTextPropType.ktptFontSize, "fontsize"),
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
        ]);
}
