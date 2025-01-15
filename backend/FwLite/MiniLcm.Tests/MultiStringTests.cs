using System.Text.Json;
using SystemTextJsonPatch;

namespace MiniLcm.Tests;

public class MultiStringTests
{

    [Fact]
    public void CanDeserializeMultiString()
    {
        //lang=json
        var json = """{"en": "test"}""";
        var expectedMs = new MultiString() { Values = { { "en", "test" } } };
        var actualMs = JsonSerializer.Deserialize<MultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Values.Should().ContainKey("en");
        actualMs.Should().BeEquivalentTo(expectedMs);
    }

    [Fact]
    public void MultiString_DeserializesEmptyString()
    {
        //lang=json
        var json = """{"en": ""}""";
        var actualMs = JsonSerializer.Deserialize<MultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Values.Should().NotContainKey("en");
    }

    [Fact]
    public void MultiString_DeserializesWhitespaceString()
    {
        //lang=json
        var json = """{"en": "\t"}""";
        var actualMs = JsonSerializer.Deserialize<MultiString>(json);
        actualMs.Should().NotBeNull();
        actualMs.Values.Should().ContainKey("en");
    }

    [Fact]
    public void JsonPatchCanUpdateMultiString()
    {
        var ms = new MultiString() { Values = { { "en", "test" } } };
        var patch = new JsonPatchDocument<MultiString>();
        patch.Replace(ms => ms["en"], "updated");
        patch.ApplyTo(ms);
        ms.Values.Should().ContainKey("en");
        ms.Values["en"].Should().Be("updated");
    }

    [Fact]
    public void JsonPatchCanRemoveMultiString()
    {
        var ms = new MultiString() { Values = { { "en", "test" } } };
        var patch = new JsonPatchDocument<MultiString>();
        patch.Remove(ms => ms["en"]);
        patch.ApplyTo(ms);
        ms.Values.Should().NotContainKey("en");
    }

    [Fact]
    public void JsonPatchCanAddMultiString()
    {
        var ms = new MultiString() { Values = { { "en", "test" } } };
        var patch = new JsonPatchDocument<MultiString>();
        patch.Add(ms => ms["fr"], "test");
        patch.ApplyTo(ms);
        ms.Values.Should().ContainKey("fr");
        ms.Values["fr"].Should().Be("test");
    }
}
