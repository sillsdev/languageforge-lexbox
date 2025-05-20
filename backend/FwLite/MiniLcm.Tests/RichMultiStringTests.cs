using System.Text.Json;
using SystemTextJsonPatch;

namespace MiniLcm.Tests;

public class RichMultiStringTests
{

    [Fact(Skip = "disabled until we can migrate data to the new format")]
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
    public void RichMultiString_SerializesToJson()
    {
        //lang=json
        var expectedJson = """{"en":"test"}""";
        var ms = new RichMultiString() { { "en", new RichString("test") } };
        var actualJson = JsonSerializer.Serialize(ms);
        actualJson.Should().Be(expectedJson);
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
        ms["fr"].Should().Be("test");
    }
}
