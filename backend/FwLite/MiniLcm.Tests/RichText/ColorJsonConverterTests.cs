using System.Drawing;
using System.Text.Json;
using MiniLcm.RichText;

namespace MiniLcm.Tests.RichText;

public class ColorJsonConverterTests
{
    private static readonly JsonSerializerOptions ColorJsonOptions = new(JsonSerializerDefaults.General)
    {
        Converters = { new ColorJsonConverter() }
    };

    [Fact]
    public void WritesTransparentToHexA()
    {
        Color? color = Color.Transparent;
        JsonSerializer.Serialize(color, ColorJsonOptions).Should().Be("\"#00000000\"");
    }

    [Fact]
    public void ReadsTransparentToAnUnNamedColor()
    {
        var color = JsonSerializer.Deserialize<Color?>("\"#00000000\"", ColorJsonOptions);
        color.Should().NotBeNull();
        color.Value.IsKnownColor.Should().BeFalse();
        color.Value.IsNamedColor.Should().BeFalse();
        color.Value.IsSystemColor.Should().BeFalse();
    }
}
