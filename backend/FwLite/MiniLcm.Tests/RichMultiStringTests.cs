using System.Text.Json;
using SystemTextJsonPatch;

namespace MiniLcm.Tests;

public class RichMultiStringTests
{
    [Theory]
    [InlineData("<span>test</span>")]
    [InlineData("<span>test</span><span>test1</span>")]
    public void ValidValues(string value)
    {
        RichMultiString.IsValidRichString(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("test")]
    [InlineData("test<span>test</span>")]
    [InlineData("<span>test</span>test")]
    [InlineData("<span><span>inner</span></span>")]
    public void InvalidValues(string value)
    {
        RichMultiString.IsValidRichString(value).Should().BeFalse();
    }

    [Fact]
    public void CreatingARichMultiStringWithPlainTextFails()
    {
        //lang=json
        var json = """{"en": "test"}""";
        var act = () => JsonSerializer.Deserialize<RichMultiString>(json);
        act.Should().Throw<Exception>();
    }


    [Fact]
    public void CanDeserializeRichMultiString()
    {
        //lang=json
        var json = """{"en": "<span>test</span>"}""";
        var expectedMs = new RichMultiString() { { "en", "<span>test</span>" } };
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
    public void JsonPatchCanUpdateRichMultiString()
    {
        var ms = new RichMultiString() { { "en", "<span>test</span>" } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Replace(ms => ms["en"], "updated");
        patch.ApplyTo(ms);
        ms.Should().ContainKey("en");
        ms["en"].Should().Be("updated");
    }

    [Fact]
    public void JsonPatchCanRemoveRichMultiString()
    {
        var ms = new RichMultiString() { { "en", "<span>test</span>" } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Remove(ms => ms["en"]);
        patch.ApplyTo(ms);
        ms.Should().NotContainKey("en");
    }

    [Fact]
    public void JsonPatchCanAddRichMultiString()
    {
        var ms = new RichMultiString() {  { "en", "<span>test</span>" } };
        var patch = new JsonPatchDocument<RichMultiString>();
        patch.Add(ms => ms["fr"], "<span>test</span>");
        patch.ApplyTo(ms);
        ms.Should().ContainKey("fr");
        ms["fr"].Should().Be("<span>test</span>");
    }
}
