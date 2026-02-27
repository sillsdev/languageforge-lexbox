using System.Text.Json;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace MiniLcm.Tests;

public class RichMultiStringTests
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.Config);

    [Fact]
    public void RichMultiString_DeserializesSimpleRichString()
    {
        //lang=json
        var json = """{"en":{"Spans":[{"Text":"test"}]}}""";
        var expectedMs = new RichMultiString() { { "en", new RichString("test") } };
        var actualMs = JsonSerializer.Deserialize<RichMultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Should().ContainKey("en");
        actualMs.Should().BeEquivalentTo(expectedMs);
    }

    [Fact]
    public void RichMultiString_DeserializesStyledRichString()
    {
        //lang=json
        var json = """{"en":{"Spans":[{"Text":"test","Bold":"On"}]}}""";
        var expectedMs = new RichMultiString() { { "en", new RichString([new RichSpan()
        {
            Text = "test",
            Bold = RichTextToggle.On
        }]) } };
        var actualMs = JsonSerializer.Deserialize<RichMultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Should().ContainKey("en");
        actualMs.Should().BeEquivalentTo(expectedMs);
    }

    [Fact]
    public void RichMultiString_DeserializesString()
    {
        //lang=json
        var json = """{"en": "test"}""";
        var expectedMs = new RichMultiString() { { "en", new RichString("test") } };
        var actualMs = JsonSerializer.Deserialize<RichMultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Should().ContainKey("en");
        actualMs.Should().BeEquivalentTo(expectedMs);
    }

    [Fact]
    public void RichMultiString_DeserializesEmptyString()
    {
        //lang=json
        var json = """{"en": ""}""";
        var actualMs = JsonSerializer.Deserialize<RichMultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Should().NotContainKey("en");
    }

    [Fact]
    public void RichMultiString_DeserializesWhitespaceString()
    {
        //lang=json
        var json = """{"en": "\t"}""";
        var actualMs = JsonSerializer.Deserialize<RichMultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Should().NotContainKey("en");
    }

    [Fact]
    public void RichMultiString_SimpleSpanSerializesToJson()
    {
        //lang=json
        var expectedJson = """{"en":{"Spans":[{"Text":"test","Ws":"en"}]}}""";
        var ms = new RichMultiString() { { "en", new RichString("test", "en") } };
        var actualJson = JsonSerializer.Serialize(ms);
        actualJson.Should().Be(expectedJson);
    }

    [Fact]
    public void RichMultiString_StyledSpanSerializesToJson()
    {
        //lang=json
        var expectedJson = """{"en":{"Spans":[{"Text":"test","Ws":"en","Bold":"On"}]}}""";
        var ms = new RichMultiString() { { "en", new RichString([new RichSpan()
        {
            Text = "test",
            Ws = "en",
            Bold = RichTextToggle.On
        }]) } };
        var actualJson = JsonSerializer.Serialize(ms);
        actualJson.Should().Be(expectedJson);
    }

    [Fact]
    public void RichMultiString_RoundTripEquals()
    {
        var ms = new RichMultiString()
        {
            { "en", new RichString([new RichSpan() { Text = "test", Ws = "en", Bold = RichTextToggle.On }]) }
        };
        var json = JsonSerializer.Serialize(ms);
        var actualMs = JsonSerializer.Deserialize<RichMultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Should().BeEquivalentTo(ms);
    }

    [Fact]
    public void RichMultiString_RoundTripRandom()
    {
        var ms = AutoFaker.Generate<RichMultiString>();
        var json = JsonSerializer.Serialize(ms);
        var actualMs = JsonSerializer.Deserialize<RichMultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Should().BeEquivalentTo(ms);
    }

    [Fact]
    public void RichString_RoundTripRandom()
    {
        var richString = AutoFaker.Generate<RichString>();
        var json = JsonSerializer.Serialize(richString);
        var actualRichString = JsonSerializer.Deserialize<RichString>(json);
        actualRichString.Should().NotBeNull();
        actualRichString.Should().BeEquivalentTo(richString);
    }

    [Fact]
    public void RichSpan_RoundTripRandom()
    {
        var richSpan = AutoFaker.Generate<RichSpan>();
        richSpan.Should().NotBeNull();
        var json = JsonSerializer.Serialize(richSpan);
        var actualRichSpan = JsonSerializer.Deserialize<RichSpan>(json);
        actualRichSpan.Should().NotBeNull();
        actualRichSpan.Should().BeEquivalentTo(richSpan);
    }

    [Fact]
    public void RichSpan_EqualityTests()
    {
        var richSpan = AutoFaker.Generate<RichSpan>();
        var actualSpan = richSpan.Copy();
        richSpan.Should().Be(actualSpan);
        richSpan.Equals(actualSpan).Should().BeTrue();
        richSpan.GetHashCode().Should().Be(actualSpan.GetHashCode());
    }

    [Fact]
    public void RichSpan_EqualityTextMustMatch()
    {
        var span1 = new RichSpan() { Text = "test1" };
        var span2 = new RichSpan() { Text = "test2" };
        span1.Should().NotBe(span2);
        span1.Equals(span2).Should().BeFalse();
        var span3 = new RichSpan() { Text = "test1" };
        span3.Equals(span1).Should().BeTrue();
    }

    [Fact]
    public void RichSpan_EqualityObjectDataMustMatch()
    {
        var span1 = new RichSpan()
        {
            Text = "test1",
            ObjData = new RichTextObjectData() { Value = "test1", Type = RichTextObjectDataType.ContextString }
        };
        var span2 = new RichSpan()
        {
            Text = "test2",
            ObjData = new RichTextObjectData() { Value = "test2", Type = RichTextObjectDataType.ContextString }
        };
        span1.Should().NotBe(span2);
        span1.Equals(span2).Should().BeFalse();
        var span3 = new RichSpan()
        {
            Text = "test1",
            ObjData = new RichTextObjectData() { Value = "test1", Type = RichTextObjectDataType.ContextString }
        };
        span3.Equals(span1).Should().BeTrue();
    }

    [Fact]
    public void RichSpan_PerPropertyEqualityTests()
    {
        static object GenerateDifferentValue(AutoFaker autoFaker, Type type, object? currentValue)
        {
            // AutoBogus can return null/default; retry until we actually change the field so the test is a reliable tripwire.
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var value = autoFaker.Generate(type);
                if (value is null) continue;
                if (currentValue is null || !currentValue.Equals(value)) return value;
            }

            throw new InvalidOperationException($"Unable to generate a different value for type '{type.FullName}'.");
        }

        var blankSpan = new RichSpan()
        {
            Text = "test"
        };
        foreach (var fieldInfo in typeof(RichSpan).GetFields())
        {
            //need to do a better test for this type
            if (fieldInfo.FieldType == typeof(RichTextObjectData)) continue;
            var span = new RichSpan() { Text = "test" };
            span.Equals(blankSpan).Should().BeTrue();
            var currentValue = fieldInfo.GetValue(span);
            var differentValue = GenerateDifferentValue(AutoFaker, fieldInfo.FieldType, currentValue);
            fieldInfo.SetValue(span, differentValue);
            span.Equals(blankSpan).Should().BeFalse("field {0} do not match", fieldInfo.Name);
        }
    }

    [Fact]
    public void RichString_HashCodeMatchesEquality()
    {
        var rs1 = new RichString([new RichSpan() { Text = "test", Ws = "en", Bold = RichTextToggle.On }]);
        var rs2 = rs1.Copy();

        rs1.Equals(rs2).Should().BeTrue();
        rs1.GetHashCode().Should().Be(rs2.GetHashCode());
    }

    [Fact]
    public void RichString_HashCodeMatchesEquality_ManySeededCases()
    {
        var seededFaker = new AutoFaker(AutoFakerDefault.Config);
        seededFaker.UseSeed(12345);
        for (var i = 0; i < 200; i++)
        {
            var rs1 = seededFaker.Generate<RichString>();
            var rs2 = rs1.Copy();

            rs1.Equals(rs2).Should().BeTrue();
            rs1.GetHashCode().Should().Be(rs2.GetHashCode());
        }
    }

    [Fact]
    public void JsonPatchCanUpdateRichMultiString()
    {
        var ms = new RichMultiString() { { "en", new RichString("test") } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Replace(ms => ms["en"], new RichString("updated"));
        patch.ApplyTo(ms);
        ms.Should().ContainKey("en");
        ms["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
    }

    //this test emulates any existing data that was stored as a string
    [Fact]
    public void JsonPatchCanUpdateRichMultiStringWhenValueIsString()
    {
        var ms = new RichMultiString() { { "en", new RichString("test") } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Operations.Add(new Operation<RichMultiString>("replace", "/en", null, "updated"));
        patch.ApplyTo(ms);
        ms.Should().ContainKey("en");
        ms["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
    }

    [Fact]
    public void JsonPatchCanUpdateRichMultiStringWhenValueIsEmptyString()
    {
        var ms = new RichMultiString() { { "en", new RichString("test") } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Operations.Add(new Operation<RichMultiString>("replace", "/en", null, ""));
        patch.ApplyTo(ms);
        ms.Should().BeEmpty();
    }

    [Fact]
    public void JsonPatchCanRemoveRichMultiString()
    {
        var ms = new RichMultiString() { { "en", new RichString("test") } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Remove(ms => ms["en"]);
        patch.ApplyTo(ms);
        ms.Should().NotContainKey("en");
    }

    [Fact]
    public void JsonPatchCanAddRichMultiString()
    {
        var ms = new RichMultiString() {  { "en", new RichString("test", "en") } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Add(ms => ms["fr"], new RichString("test", "fr"));
        patch.ApplyTo(ms);
        ms.Should().ContainKey("fr");
        ms["fr"].Should().BeEquivalentTo(new RichString("test", "fr"));
    }

    [Fact]
    public void RichSpanEquality_TrueWhenMatching()
    {
        var span = new RichSpan() { Text = "test", Bold = RichTextToggle.On};
        var spanCopy = new RichSpan() { Text = "test", Bold = RichTextToggle.On};
        span.Equals(spanCopy).Should().BeTrue();
    }

    [Fact]
    public void RichSpanEquality_FalseWhenTextDiffers()
    {
        var span = new RichSpan() { Text = "test1", Bold = RichTextToggle.On};
        var spanCopy = new RichSpan() { Text = "test2", Bold = RichTextToggle.On};
        span.Equals(spanCopy).Should().BeFalse();
    }

    [Fact]
    public void RichSpanEquality_FalseWhenBoldDiffers()
    {
        var span = new RichSpan() { Text = "test", Bold = RichTextToggle.On};
        var spanCopy = new RichSpan() { Text = "test", Bold = RichTextToggle.Off};
        span.Equals(spanCopy).Should().BeFalse();
    }

    [Fact]
    public void RichStringMergesSpansWithTheSameProps()
    {
        var span1 = new RichSpan() { Text = "span1", Ws = "en", Bold = RichTextToggle.On };
        var span2 = new RichSpan() { Text = "span2", Ws = "en", Bold = RichTextToggle.On };
        var richString = new RichString([span1, span2]);
        richString.Should()
            .BeEquivalentTo(
                new RichString([new RichSpan() { Text = "span1span2", Ws = "en", Bold = RichTextToggle.On }]));
    }
}
