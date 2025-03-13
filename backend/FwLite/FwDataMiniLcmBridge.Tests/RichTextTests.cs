using System.Globalization;
using FluentAssertions.Execution;
using FwDataMiniLcmBridge.Api;
using MiniLcm.Models;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using Xunit.Abstractions;

namespace FwDataMiniLcmBridge.Tests;

public class RichTextTests(ITestOutputHelper output)
{
    private const int FakeWsHandleEn = 346;

    [Fact]
    public void AllFwTextPropTypesAreMapped()
    {
        FwTextPropType[] excludedTypes = [FwTextPropType.ktptMarkItem];
        var allTypes = Enum.GetValues<FwTextPropType>();
        var notMapped = allTypes
            .Except(excludedTypes)
            .Except(RichTextMapping.PropTypeMap.Keys)
            .ToArray();
        if (notMapped.Length > 0) output.WriteLine($"Not mapped: {string.Join(", ", notMapped)}");
        notMapped.Should().BeEmpty();
    }

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
        IEnumerable<(FwTextPropType propType, object? value, int variation, Action<ITsTextProps, RichSpan> assert)> GetData()
        {
            yield return (FwTextPropType.ktptWs, FakeWsHandleEn, 0, (props, span) => span.Ws.Should().Be((WritingSystemId)"en"));
            //may show up as FontFamily in test output
            yield return (FwTextPropType.ktptBaseWs, FakeWsHandleEn, 0, (props, span) => span.WsBase.Should().Be((WritingSystemId)"en"));

            //may show up as CharStyle in test output
            yield return (FwTextPropType.ktptItalic, null, 0, (props, span) => span.Italic.Should().BeNull());
            yield return (FwTextPropType.ktptItalic, FwTextToggleVal.kttvOff, 0, (props, span) => span.Italic.Should().Be(RichTextToggle.Off));
            yield return (FwTextPropType.ktptItalic, FwTextToggleVal.kttvForceOn, 0, (props, span) => span.Italic.Should().Be(RichTextToggle.On));
            yield return (FwTextPropType.ktptItalic, FwTextToggleVal.kttvInvert, 0, (props, span) => span.Italic.Should().Be(RichTextToggle.Invert));

            //may show up as ParaStyle in test output
            yield return (FwTextPropType.ktptBold, null, 0, (props, span) => span.Bold.Should().BeNull());
            yield return (FwTextPropType.ktptBold, FwTextToggleVal.kttvOff, 0, (props, span) => span.Bold.Should().Be(RichTextToggle.Off));
            yield return (FwTextPropType.ktptBold, FwTextToggleVal.kttvForceOn, 0, (props, span) => span.Bold.Should().Be(RichTextToggle.On));
            yield return (FwTextPropType.ktptBold, FwTextToggleVal.kttvInvert, 0, (props, span) => span.Bold.Should().Be(RichTextToggle.Invert));

            //may show up as TabList in test output
            yield return (FwTextPropType.ktptSuperscript, null, 0, (props, span) => span.Superscript.Should().BeNull());
            yield return (FwTextPropType.ktptSuperscript, FwSuperscriptVal.kssvOff, 0, (props, span) => span.Superscript.Should().Be(RichTextSuperscript.None));
            yield return (FwTextPropType.ktptSuperscript, FwSuperscriptVal.kssvSuper, 0, (props, span) => span.Superscript.Should().Be(RichTextSuperscript.Superscript));
            yield return (FwTextPropType.ktptSuperscript, FwSuperscriptVal.kssvSub, 0, (props, span) => span.Superscript.Should().Be(RichTextSuperscript.Subscript));

            //may show up as Tags in the test output
            yield return (FwTextPropType.ktptUnderline, null, 0, (props, span) => span.Underline.Should().BeNull());
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntNone, 0, (props, span) => span.Underline.Should().Be(RichTextUnderline.None));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntDotted, 0, (props, span) => span.Underline.Should().Be(RichTextUnderline.Dotted));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntDashed, 0, (props, span) => span.Underline.Should().Be(RichTextUnderline.Dashed));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntSingle, 0, (props, span) => span.Underline.Should().Be(RichTextUnderline.Single));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntDouble, 0, (props, span) => span.Underline.Should().Be(RichTextUnderline.Double));
            yield return (FwTextPropType.ktptUnderline, FwUnderlineType.kuntSquiggle, 0, (props, span) => span.Underline.Should().Be(RichTextUnderline.Squiggle));

            //may show up as ObjData
            yield return (FwTextPropType.ktptFontSize, null, 0, (props, span) => span.FontSize.Should().BeNull());
            yield return (FwTextPropType.ktptFontSize, 23, 0, (props, span) => span.FontSize.Should().Be(23));

            //may show up as CustomBullet
            yield return (FwTextPropType.ktptOffset, null, 0, (props, span) => span.Offset.Should().BeNull());
            yield return (FwTextPropType.ktptOffset, 23, 0, (props, span) => span.Offset.Should().Be(23));

            //colors
            //SIL blue
            var silBlueInt = 12147200;
            var silBlueHex = "#005ab9";
            yield return (FwTextPropType.ktptForeColor, null, 0, (props, span) => span.ForeColor.Should().BeNull());
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrWhite, 0, (props, span) => span.ForeColor.Should().Be("#ffffff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrBlack, 0, (props, span) => span.ForeColor.Should().Be("#000000"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrRed, 0, (props, span) => span.ForeColor.Should().Be("#ff0000"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrGreen, 0, (props, span) => span.ForeColor.Should().Be("#00ff00"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrBlue, 0, (props, span) => span.ForeColor.Should().Be("#0000ff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrYellow, 0, (props, span) => span.ForeColor.Should().Be("#ffff00"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrMagenta, 0, (props, span) => span.ForeColor.Should().Be("#ff00ff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrCyan, 0, (props, span) => span.ForeColor.Should().Be("#00ffff"));
            yield return (FwTextPropType.ktptForeColor, FwTextColor.kclrTransparent, 0, (props, span) => span.ForeColor.Should().Be("#00000000"));
            yield return (FwTextPropType.ktptForeColor, silBlueInt, 0, (props, span) => span.ForeColor.Should().Be(silBlueHex));

            yield return (FwTextPropType.ktptBackColor, null, 0, (props, span) => span.BackColor.Should().BeNull());
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrWhite, 0, (props, span) => span.BackColor.Should().Be("#ffffff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrBlack, 0, (props, span) => span.BackColor.Should().Be("#000000"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrRed, 0, (props, span) => span.BackColor.Should().Be("#ff0000"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrGreen, 0, (props, span) => span.BackColor.Should().Be("#00ff00"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrBlue, 0, (props, span) => span.BackColor.Should().Be("#0000ff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrYellow, 0, (props, span) => span.BackColor.Should().Be("#ffff00"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrMagenta, 0, (props, span) => span.BackColor.Should().Be("#ff00ff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrCyan, 0, (props, span) => span.BackColor.Should().Be("#00ffff"));
            yield return (FwTextPropType.ktptBackColor, FwTextColor.kclrTransparent, 0, (props, span) => span.BackColor.Should().Be("#00000000"));
            yield return (FwTextPropType.ktptBackColor, silBlueInt, 0, (props, span) => span.BackColor.Should().Be(silBlueHex));

            yield return (FwTextPropType.ktptUnderColor, null, 0, (props, span) => span.UnderColor.Should().BeNull());
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrWhite, 0, (props, span) => span.UnderColor.Should().Be("#ffffff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrBlack, 0, (props, span) => span.UnderColor.Should().Be("#000000"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrRed, 0, (props, span) => span.UnderColor.Should().Be("#ff0000"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrGreen, 0, (props, span) => span.UnderColor.Should().Be("#00ff00"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrBlue, 0, (props, span) => span.UnderColor.Should().Be("#0000ff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrYellow, 0, (props, span) => span.UnderColor.Should().Be("#ffff00"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrMagenta, 0, (props, span) => span.UnderColor.Should().Be("#ff00ff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrCyan, 0, (props, span) => span.UnderColor.Should().Be("#00ffff"));
            yield return (FwTextPropType.ktptUnderColor, FwTextColor.kclrTransparent, 0, (props, span) => span.UnderColor.Should().Be("#00000000"));
            yield return (FwTextPropType.ktptUnderColor, silBlueInt, 0, (props, span) => span.UnderColor.Should().Be(silBlueHex));


        }

        return GetData().Select(x =>
            new object?[] { x.propType, x.value, x.variation, x.assert });
    }

    [Theory]
    [MemberData(nameof(IntPropTypeIsMappedCorrectlyData))]
    //by making value an object and converting to an int we can see enum names in the test labels
    public void IntPropTypeIsMappedCorrectly(FwTextPropType propType, object? value, int variation, Action<ITsTextProps, RichSpan> assert)
    {
        var span = new RichSpan() { Text = "test" };
        var builder = TsStringUtils.MakePropsBldr();
        if (value is not null)
            builder.SetIntPropValues((int)propType, variation, Convert.ToInt32(value));
        var textProps = builder.GetTextProps();

        RichTextMapping.WriteToSpan(span, textProps, WsIdLookup);
        assert(textProps, span);
    }

    public static IEnumerable<object?[]> StringPropTypeIsMappedCorrectlyData()
    {
        IEnumerable<(FwTextPropType propType, string? value, Action<ITsTextProps, RichSpan> assert)> GetData()
        {
            yield return (FwTextPropType.ktptNamedStyle, null, (props, span) => span.NamedStyle.Should().BeNull());
            yield return (FwTextPropType.ktptNamedStyle, "Strong", (props, span) => span.NamedStyle.Should().Be("Strong"));
            yield return (FwTextPropType.ktptCharStyle, null, (props, span) => span.CharStyle.Should().BeNull());
            yield return (FwTextPropType.ktptCharStyle, "SomeString", (props, span) => span.CharStyle.Should().Be("SomeString"));
        }
        return GetData().Select(x => new object?[] { x.propType, x.value, x.assert });
    }

    [Theory]
    [MemberData(nameof(StringPropTypeIsMappedCorrectlyData))]
    public void StringPropTypeIsMappedCorrectly(FwTextPropType propType, string value, Action<ITsTextProps, RichSpan> assert)
    {
        var span = new RichSpan() { Text = "test" };
        var builder = TsStringUtils.MakePropsBldr();
        builder.SetStrPropValue((int)propType, value);
        var textProps = builder.GetTextProps();

        RichTextMapping.WriteToSpan(span, textProps, WsIdLookup);
        assert(textProps, span);
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
        notTested.Should().BeEmpty();

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
