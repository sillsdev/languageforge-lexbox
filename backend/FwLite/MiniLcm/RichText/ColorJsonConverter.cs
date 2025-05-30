using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniLcm.RichText;

public class ColorJsonConverter : JsonConverter<Color?>
{
    private const string Transparent = "#00000000";
    //can't use Color.Transparent because our equality tests will fail
    public static readonly Color UnnamedTransparent = Color.FromArgb(Color.Transparent.ToArgb());
    public override Color? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var htmlColor = reader.GetString();
        if (string.IsNullOrEmpty(htmlColor)) return null;
        if (htmlColor == Transparent) return UnnamedTransparent;
        return ColorTranslator.FromHtml(htmlColor);
    }

    public override void Write(Utf8JsonWriter writer, Color? value, JsonSerializerOptions options)
    {
        if (value == null || value.Value == Color.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        var color = value.Value;
        if (color.A == 0)
        {
            //need to do this, otherwise we lose the transparency
            writer.WriteStringValue(Transparent);
            return;
        }
        //not using ToHtml because that will use named colors, which we don't want
        writer.WriteStringValue($"#{color.R:X2}{color.G:X2}{color.B:X2}");
    }
}
