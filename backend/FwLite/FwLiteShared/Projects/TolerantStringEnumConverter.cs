using System.Text.Json;
using System.Text.Json.Serialization;

namespace FwLiteShared.Projects;

/// <summary>
/// Like <see cref="JsonStringEnumConverter"/>, but reads a name it doesn't know as <c>default(T)</c> instead of throwing.
/// For enums the server can extend, so an older client reading a newer value doesn't fail the whole response.
/// Not used on the shared DTOs themselves: the server's OpenAPI spec only describes the built-in string enum converter.
/// </summary>
public class TolerantStringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String when Enum.TryParse<T>(reader.GetString(), ignoreCase: true, out var value) => value,
            JsonTokenType.Number when reader.TryGetInt32(out var number) => (T)Enum.ToObject(typeof(T), number),
            JsonTokenType.String or JsonTokenType.Number => default,
            _ => throw new JsonException($"Unexpected token {reader.TokenType} for enum {typeof(T).Name}")
        };
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
