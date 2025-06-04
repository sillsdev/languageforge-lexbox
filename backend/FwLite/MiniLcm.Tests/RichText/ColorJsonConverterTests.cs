using System.Drawing;
using System.Text.Json;
using MiniLcm.RichText;

namespace MiniLcm.Tests.RichText;

public class ColorJsonConverterTests
{
    private JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.General)
    {
        Converters = { new ColorJsonConverter() }
    };

    [Fact]
    public void WritesTransparentToHexA()
    {
        Color? color = Color.Transparent;
        JsonSerializer.Serialize(color, options).Should().Be("\"#00000000\"");
    }

    [Fact]
    public void ReadsTransparentToAnUnNamedColor()
    {
        var color = JsonSerializer.Deserialize<Color?>("\"#00000000\"", options);
        color.Should().NotBeNull();
        color.Value.IsKnownColor.Should().BeFalse();
        color.Value.IsNamedColor.Should().BeFalse();
        color.Value.IsSystemColor.Should().BeFalse();
    }
}
