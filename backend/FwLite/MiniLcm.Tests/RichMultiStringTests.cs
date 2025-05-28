using System.Text.Json;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace MiniLcm.Tests;

public class RichMultiStringTests
{
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
    public void JsonPatchCanUpdateRichMultiString()
    {
        var ms = new RichMultiString() { { "en", new RichString("test") } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Replace(ms => ms["en"], new RichString("updated"));
        patch.ApplyTo(ms);
        ms.Should().ContainKey("en");
        ms["en"].Should().BeEquivalentTo(new RichString("updated"));
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
        ms["en"].Should().BeEquivalentTo(new RichString("updated"));
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
        var ms = new RichMultiString() {  { "en", new RichString("test") } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Add(ms => ms["fr"], new RichString("test"));
        patch.ApplyTo(ms);
        ms.Should().ContainKey("fr");
        ms["fr"].Should().BeEquivalentTo(new RichString("test"));
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
}
