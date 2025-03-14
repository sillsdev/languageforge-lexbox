﻿using System.Globalization;
using System.Text;
using FluentAssertions.Execution;
using FwDataMiniLcmBridge.Api;
using MiniLcm.Models;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;
using Xunit.Abstractions;

namespace FwDataMiniLcmBridge.Tests;

public class RichTextTests(ITestOutputHelper output)
{
    private const int FakeWsHandleEn = 346;

    private ITsPropsBldr MakeFilledProps()
    {
        var builder = TsStringUtils.MakePropsBldr();
        builder.SetIntPropValues((int)FwTextPropType.ktptWs, 0, FakeWsHandleEn);
        builder.SetIntPropValues((int)FwTextPropType.ktptBaseWs, 0, FakeWsHandleEn);

        builder.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Strong");
        return builder;
    }

    [Fact]
    public void CanMapTextPropsToRichSpan()
    {
        var textProps = MakeFilledProps().GetTextProps();
        var span = new RichSpan() { Text = "test" };

        RichTextMapping.WriteToSpan(span, textProps, WsIdLookup);

        span.Text.Should().Be("test");
        span.Ws.Code.Should().Be("en");
        span.WsBase.GetValueOrDefault().Code.Should().Be("en");
        span.NamedStyle.Should().Be("Strong");
    }

    [Fact]
    public void CanMapSpanToTextProps()
    {
        var span = new RichSpan() { Text = "test", Ws = "en", WsBase = "en", NamedStyle = "Strong" };
        var builder = TsStringUtils.MakePropsBldr();

        RichTextMapping.WriteToTextProps(span, builder, ws => ws == "en" ? FakeWsHandleEn : throw new ArgumentException("no ws handle"));
        var textProps = builder.GetTextProps();
        textProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle).Should().Be("Strong");
        textProps.GetIntPropValues((int)FwTextPropType.ktptWs, out _).Should().Be(FakeWsHandleEn);
        textProps.GetIntPropValues((int)FwTextPropType.ktptBaseWs, out _).Should().Be(FakeWsHandleEn);
    }

    public static IEnumerable<object?[]> IntPropTypeIsMappedCorrectlyData()
    {
        IEnumerable<(FwTextPropType propType, object? value, int variation, Action<RichSpan> assert)> GetData()
        {
            //may show up as FontFamily in test output
            yield return (FwTextPropType.ktptWs, null, 0, span => span.Ws.Should().Be(default(WritingSystemId)));
            yield return (FwTextPropType.ktptWs, FakeWsHandleEn, 0, span => span.Ws.Should().Be((WritingSystemId)"en"));

            //may show up as CharStyle in test output
            yield return (FwTextPropType.ktptItalic, null, 0, span => span.Italic.Should().BeNull());
            yield return (FwTextPropType.ktptItalic, FwTextToggleVal.kttvOff, 0, span => span.Italic.Should().Be(RichTextToggle.Off));
            yield return (FwTextPropType.ktptItalic, FwTextToggleVal.kttvForceOn, 0, span => span.Italic.Should().Be(RichTextToggle.On));
            yield return (FwTextPropType.ktptItalic, FwTextToggleVal.kttvInvert, 0, span => span.Italic.Should().Be(RichTextToggle.Invert));

            //may show up as ParaStyle in test output
            yield return (FwTextPropType.ktptBold, null, 0, span => span.Bold.Should().BeNull());
            yield return (FwTextPropType.ktptBold, FwTextToggleVal.kttvOff, 0, span => span.Bold.Should().Be(RichTextToggle.Off));
            yield return (FwTextPropType.ktptBold, FwTextToggleVal.kttvForceOn, 0, span => span.Bold.Should().Be(RichTextToggle.On));
            yield return (FwTextPropType.ktptBold, FwTextToggleVal.kttvInvert, 0, span => span.Bold.Should().Be(RichTextToggle.Invert));

            //may show up as TabList in test output
            yield return (FwTextPropType.ktptSuperscript, null, 0, span => span.Superscript.Should().BeNull());
            yield return (FwTextPropType.ktptSuperscript, FwSuperscriptVal.kssvOff, 0, span => span.Superscript.Should().Be(RichTextSuperscript.None));
            yield return (FwTextPropType.ktptSuperscript, FwSuperscriptVal.kssvSuper, 0, span => span.Superscript.Should().Be(RichTextSuperscript.Superscript));
            yield return (FwTextPropType.ktptSuperscript, FwSuperscriptVal.kssvSub, 0, span => span.Superscript.Should().Be(RichTextSuperscript.Subscript));

            //may show up as Tags in the test output
            yield return (FwTextPropType.ktptUnderline, null, 0, span => span.Underline.Should().BeNull());
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntNone, 0, span => span.Underline.Should().Be(RichTextUnderline.None));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntDotted, 0, span => span.Underline.Should().Be(RichTextUnderline.Dotted));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntDashed, 0, span => span.Underline.Should().Be(RichTextUnderline.Dashed));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntSingle, 0, span => span.Underline.Should().Be(RichTextUnderline.Single));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntDouble, 0, span => span.Underline.Should().Be(RichTextUnderline.Double));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntStrikethrough, 0, span => span.Underline.Should().Be(RichTextUnderline.Strikethrough));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntSquiggle, 0, span => span.Underline.Should().Be(RichTextUnderline.Squiggle));

            //may show up as ObjData
            yield return (FwTextPropType.ktptFontSize, null, 0, span => span.FontSize.Should().BeNull());
            yield return (FwTextPropType.ktptFontSize, 23, 0, span => span.FontSize.Should().Be(23));

            //may show up as CustomBullet
            yield return (FwTextPropType.ktptOffset, null, 0, span => span.Offset.Should().BeNull());
            yield return (FwTextPropType.ktptOffset, 23, 0, span => span.Offset.Should().Be(23));

            //colors
            //SIL blue
            var silBlueInt = 12147200;
            var silBlueHex = "#005ab9";
            yield return (FwTextPropType.ktptForeColor, null, 0, span => span.ForeColor.Should().BeNull());
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrWhite, 0, span => span.ForeColor.Should().Be("#ffffff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrBlack, 0, span => span.ForeColor.Should().Be("#000000"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrRed, 0, span => span.ForeColor.Should().Be("#ff0000"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrGreen, 0, span => span.ForeColor.Should().Be("#00ff00"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrBlue, 0, span => span.ForeColor.Should().Be("#0000ff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrYellow, 0, span => span.ForeColor.Should().Be("#ffff00"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrMagenta, 0, span => span.ForeColor.Should().Be("#ff00ff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrCyan, 0, span => span.ForeColor.Should().Be("#00ffff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrTransparent, 0, span => span.ForeColor.Should().Be("#00000000"));
            yield return (FwTextPropType.ktptForeColor, silBlueInt, 0, span => span.ForeColor.Should().Be(silBlueHex));

            yield return (FwTextPropType.ktptBackColor, null, 0, span => span.BackColor.Should().BeNull());
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrWhite, 0, span => span.BackColor.Should().Be("#ffffff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrBlack, 0, span => span.BackColor.Should().Be("#000000"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrRed, 0, span => span.BackColor.Should().Be("#ff0000"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrGreen, 0, span => span.BackColor.Should().Be("#00ff00"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrBlue, 0, span => span.BackColor.Should().Be("#0000ff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrYellow, 0, span => span.BackColor.Should().Be("#ffff00"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrMagenta, 0, span => span.BackColor.Should().Be("#ff00ff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrCyan, 0, span => span.BackColor.Should().Be("#00ffff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrTransparent, 0, span => span.BackColor.Should().Be("#00000000"));
            yield return (FwTextPropType.ktptBackColor, silBlueInt, 0, span => span.BackColor.Should().Be(silBlueHex));

            yield return (FwTextPropType.ktptUnderColor, null, 0, span => span.UnderColor.Should().BeNull());
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrWhite, 0, span => span.UnderColor.Should().Be("#ffffff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrBlack, 0, span => span.UnderColor.Should().Be("#000000"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrRed, 0, span => span.UnderColor.Should().Be("#ff0000"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrGreen, 0, span => span.UnderColor.Should().Be("#00ff00"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrBlue, 0, span => span.UnderColor.Should().Be("#0000ff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrYellow, 0, span => span.UnderColor.Should().Be("#ffff00"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrMagenta, 0, span => span.UnderColor.Should().Be("#ff00ff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrCyan, 0, span => span.UnderColor.Should().Be("#00ffff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrTransparent, 0, span => span.UnderColor.Should().Be("#00000000"));
            yield return (FwTextPropType.ktptUnderColor, silBlueInt, 0, span => span.UnderColor.Should().Be(silBlueHex));

            yield return (FwTextPropType.ktptBaseWs, null, 0, span => span.WsBase.Should().BeNull());
            yield return (FwTextPropType.ktptBaseWs, FakeWsHandleEn, 0, span => span.WsBase.Should().Be((WritingSystemId)"en"));

            yield return (FwTextPropType.ktptAlign, null, 0, span => span.Align.Should().BeNull());
            yield return (FwTextPropType.ktptAlign, FwTextAlign.ktalLeading, 0, span => span.Align.Should().Be(RichTextAlign.Leading));
            yield return (FwTextPropType.ktptAlign, FwTextAlign.ktalLeft, 0, span => span.Align.Should().Be(RichTextAlign.Left));
            yield return (FwTextPropType.ktptAlign, FwTextAlign.ktalCenter, 0, span => span.Align.Should().Be(RichTextAlign.Center));
            yield return (FwTextPropType.ktptAlign, FwTextAlign.ktalRight, 0, span => span.Align.Should().Be(RichTextAlign.Right));
            yield return (FwTextPropType.ktptAlign, FwTextAlign.ktalTrailing, 0, span => span.Align.Should().Be(RichTextAlign.Trailing));
            yield return (FwTextPropType.ktptAlign, FwTextAlign.ktalJustify, 0, span => span.Align.Should().Be(RichTextAlign.Justify));
            yield return (FwTextPropType.ktptAlign, 2345, 0, span => span.Align.Should().Be((RichTextAlign)2345));

            yield return (FwTextPropType.ktptFirstIndent, null, 0, span => span.FirstIndent.Should().BeNull());
            yield return (FwTextPropType.ktptFirstIndent, 2345, 0, span => span.FirstIndent.Should().Be(2345));

            yield return (FwTextPropType.ktptLeadingIndent, null, 0, span => span.LeadingIndent.Should().BeNull());
            yield return (FwTextPropType.ktptLeadingIndent, 2345, 0, span => span.LeadingIndent.Should().Be(2345));
            yield return (FwTextPropType.ktptTrailingIndent, null, 0, span => span.TrailingIndent.Should().BeNull());
            yield return (FwTextPropType.ktptTrailingIndent, 2345, 0, span => span.TrailingIndent.Should().Be(2345));
            yield return (FwTextPropType.ktptSpaceBefore, null, 0, span => span.SpaceBefore.Should().BeNull());
            yield return (FwTextPropType.ktptSpaceBefore, 2345, 0, span => span.SpaceBefore.Should().Be(2345));
            yield return (FwTextPropType.ktptSpaceAfter, null, 0, span => span.SpaceAfter.Should().BeNull());
            yield return (FwTextPropType.ktptSpaceAfter, 2345, 0, span => span.SpaceAfter.Should().Be(2345));

            yield return (FwTextPropType.ktptTabDef, null, 0, span => span.TabDef.Should().BeNull());
            yield return (FwTextPropType.ktptTabDef, 2345, 0, span => span.TabDef.Should().Be(2345));
            yield return (FwTextPropType.ktptLineHeight, null, 0, span => span.LineHeight.Should().BeNull());
            yield return (FwTextPropType.ktptLineHeight, 2345, 0, span => span.LineHeight.Should().Be(2345));


            yield return (FwTextPropType.ktptParaColor, null, 0, span => span.ParaColor.Should().BeNull());
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrWhite, 0, span => span.ParaColor.Should().Be("#ffffff"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrBlack, 0, span => span.ParaColor.Should().Be("#000000"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrRed, 0, span => span.ParaColor.Should().Be("#ff0000"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrGreen, 0, span => span.ParaColor.Should().Be("#00ff00"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrBlue, 0, span => span.ParaColor.Should().Be("#0000ff"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrYellow, 0, span => span.ParaColor.Should().Be("#ffff00"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrMagenta, 0, span => span.ParaColor.Should().Be("#ff00ff"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrCyan, 0, span => span.ParaColor.Should().Be("#00ffff"));
            yield return (FwTextPropType.ktptParaColor, FwTextColor.kclrTransparent, 0, span => span.ParaColor.Should().Be("#00000000"));
            yield return (FwTextPropType.ktptParaColor, silBlueInt, 0, span => span.ParaColor.Should().Be(silBlueHex));

            yield return (FwTextPropType.ktptSpellCheck, null, 0, span => span.SpellCheck.Should().BeNull());
            yield return (FwTextPropType.ktptSpellCheck, SpellingModes.ksmNormalCheck, 0, span => span.SpellCheck.Should().Be(RichTextSpellingMode.Normal));
            yield return (FwTextPropType.ktptSpellCheck, SpellingModes.ksmDoNotCheck, 0, span => span.SpellCheck.Should().Be(RichTextSpellingMode.DoNotCheck));
            yield return (FwTextPropType.ktptSpellCheck, SpellingModes.ksmForceCheck, 0, span => span.SpellCheck.Should().Be(RichTextSpellingMode.ForceCheck));
            yield return (FwTextPropType.ktptSpellCheck, 2345, 0, span => span.SpellCheck.Should().Be((RichTextSpellingMode)2345));


            yield return (FwTextPropType.ktptMarginTop, null, 0, span => span.MarginTop.Should().BeNull());
            yield return (FwTextPropType.ktptMarginTop, 2345, 0, span => span.MarginTop.Should().Be(2345));
        }

        return GetData().Select(x =>
            new object?[] { x.propType, x.value, x.variation, x.assert });
    }

    [Theory]
    [MemberData(nameof(IntPropTypeIsMappedCorrectlyData))]
    //by making value an object and converting to an int we can see enum names in the test labels
    public void IntPropTypeIsMappedCorrectly(FwTextPropType propType, object? value, int variation, Action<RichSpan> assert)
    {
        var span = new RichSpan() { Text = "test" };
        var builder = TsStringUtils.MakePropsBldr();
        if (value is not null)
            builder.SetIntPropValues((int)propType, variation, Convert.ToInt32(value));
        var textProps = builder.GetTextProps();

        RichTextMapping.WriteToSpan(span, textProps, WsIdLookup);
        assert(span);
    }

    private static string GetRawObjDataString(FwObjDataTypes dataType, Guid guid)
    {
        var guidObjData = TsStringUtils.GetObjData(guid, dataType);
        var builder = TsStringUtils.MakePropsBldr();
        builder.SetStrPropValueRgch((int)FwTextPropType.ktptObjData, guidObjData, guidObjData.Length);
        return builder.GetStrPropValue((int)FwTextPropType.ktptObjData);
    }

    public static IEnumerable<object?[]> StringPropTypeIsMappedCorrectlyData()
    {
        IEnumerable<(FwTextPropType propType, string? value, Action<RichSpan> assert)> GetData()
        {
            yield return (FwTextPropType.ktptFontFamily, null, span => span.FontFamily.Should().BeNull());
            yield return (FwTextPropType.ktptFontFamily, "SIL Charis", span => span.FontFamily.Should().Be("SIL Charis"));
            yield return (FwTextPropType.ktptNamedStyle, null, span => span.NamedStyle.Should().BeNull());
            yield return (FwTextPropType.ktptNamedStyle, "Strong", span => span.NamedStyle.Should().Be("Strong"));
            yield return (FwTextPropType.ktptCharStyle, null, span => span.CharStyle.Should().BeNull());
            yield return (FwTextPropType.ktptCharStyle, "SomeString", span => span.CharStyle.Should().Be("SomeString"));
            yield return (FwTextPropType.ktptParaStyle, null, span => span.ParaStyle.Should().BeNull());
            yield return (FwTextPropType.ktptParaStyle, "SomeString", span => span.ParaStyle.Should().Be("SomeString"));
            yield return (FwTextPropType.ktptTabList, null, span => span.TabList.Should().BeNull());
            yield return (FwTextPropType.ktptTabList, "SomeString", span => span.TabList.Should().Be("SomeString"));

            var tagsString = "\uFBA7\u3B88\u10C7\u4e14\uE09E\u0D3F\u06DA\u0D0A"
                             + "\u3ECA\uC4F0\uBC03\u4175\uAAB5\uF313\uB7EC\u81F4";
            Guid expectedGuid1 = new Guid("3B88FBA7-10C7-4e14-9EE0-3F0DDA060A0D");
            Guid expectedGuid2 = new Guid("C4F03ECA-BC03-4175-B5AA-13F3ECB7F481");
            yield return (FwTextPropType.ktptTags, null, span => span.Tags.Should().BeNull());
            yield return (FwTextPropType.ktptTags, tagsString, span => span.Tags.Should().Equal([expectedGuid1, expectedGuid2]));

            yield return (FwTextPropType.ktptObjData, null, span => span.ObjData.Should().BeNull());
            yield return (FwTextPropType.ktptObjData, ((char)FwObjDataTypes.kodtExternalPathName) + "https://google.com", span => span.ObjData.Should().BeEquivalentTo(new { Type = RichTextObjectDataType.ExternalPathName, DataString = "https://google.com" }));
            yield return (FwTextPropType.ktptObjData, ((char)FwObjDataTypes.kodtEmbeddedObjectData) + "<some-xml>value</some-xml>", span => span.ObjData.Should().BeEquivalentTo(new { Type = RichTextObjectDataType.EmbeddedObjectData, DataString = "<some-xml>value</some-xml>" }));
            FwObjDataTypes[] guidTypes =
            [
                FwObjDataTypes.kodtNameGuidHot,
                FwObjDataTypes.kodtOwnNameGuidHot,
                FwObjDataTypes.kodtContextString,
                FwObjDataTypes.kodtGuidMoveableObjDisp,
            ];
            foreach (var type in guidTypes)
            {
                var richType = type switch
                {
                    FwObjDataTypes.kodtNameGuidHot => RichTextObjectDataType.NameGuid,
                    FwObjDataTypes.kodtOwnNameGuidHot => RichTextObjectDataType.OwnNameGuid,
                    FwObjDataTypes.kodtContextString => RichTextObjectDataType.ContextString,
                    FwObjDataTypes.kodtGuidMoveableObjDisp => RichTextObjectDataType.GuidMoveableObjDisp,
                    _ => throw new ArgumentException("Unknown guid type")
                };
                var guid = Guid.NewGuid();
                var rawObjDataString = GetRawObjDataString(type, guid);
                var dataString = rawObjDataString.Substring(1);

                yield return (FwTextPropType.ktptObjData, rawObjDataString, span => span.ObjData.Should().BeEquivalentTo(new { Type = richType, DataString = dataString }));
            }
        }
        return GetData().Select(x => new object?[] { x.propType, x.value, x.assert });
    }

    [Theory]
    [MemberData(nameof(StringPropTypeIsMappedCorrectlyData))]
    public void StringPropTypeIsMappedCorrectly(FwTextPropType propType, string value, Action<RichSpan> assert)
    {
        var span = new RichSpan() { Text = "test" };
        var builder = TsStringUtils.MakePropsBldr();
        builder.SetStrPropValue((int)propType, value);
        var textProps = builder.GetTextProps();

        RichTextMapping.WriteToSpan(span, textProps, WsIdLookup);
        assert(span);
    }

    [Fact]
    public void AllPropTypesAreTested()
    {
        var testedStringTypes = StringPropTypeIsMappedCorrectlyData()
            .Select(arr => (FwTextPropType)arr[0]!).ToArray();
        var testedIntTypes = IntPropTypeIsMappedCorrectlyData().Select(arr => (FwTextPropType)arr[0]!).ToArray();
        var testedTypes = testedStringTypes
            .Union(testedIntTypes);

        FwTextPropType[] excludedTypes = [FwTextPropType.ktptMarkItem];
        var allTypes = Enum.GetValues<FwTextPropType>();
        var notTested = allTypes
            .Except(excludedTypes)
            .Except(testedTypes)
            .ToArray();
        if (notTested.Length > 0) output.WriteLine($"Not tested: {string.Join(", ", notTested)}");
        notTested.Should().BeEmpty("values should be tested, {0} are not", notTested.Length);

        //all these types are duplicated so must be in both string and int tested types
        HashSet<FwTextPropType> duplicatedTypes =
        [
            // ReSharper disable DuplicateKeyCollectionInitialization
            FwTextPropType.ktptWs, FwTextPropType.ktptFontFamily,

            FwTextPropType.ktptItalic, FwTextPropType.ktptCharStyle,

            FwTextPropType.ktptBold, FwTextPropType.ktptParaStyle,

            FwTextPropType.ktptSuperscript, FwTextPropType.ktptTabList,

            FwTextPropType.ktptUnderline, FwTextPropType.ktptTags,

            FwTextPropType.ktptFontSize, FwTextPropType.ktptObjData,

            FwTextPropType.ktptOffset, FwTextPropType.ktptCustomBullet
            // ReSharper restore DuplicateKeyCollectionInitialization
        ];

        //ensure that these types are all tested in both string and int tests
        duplicatedTypes.Except(testedStringTypes).Should().BeEmpty();
        duplicatedTypes.Except(testedIntTypes).Should().BeEmpty();
    }

    private WritingSystemId? WsIdLookup(int? handle)
    {
        if (handle == FakeWsHandleEn)
            return "en";
        return null;
    }
}
