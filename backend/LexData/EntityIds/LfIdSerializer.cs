using System.Text.Json;
using System.Text.Json.Serialization;

namespace LexData.EntityIds;

public class LfIdSerializerProvider : JsonConverterFactory
{
    public static LfIdSerializerProvider Instance { get; } = new LfIdSerializerProvider();

    private LfIdSerializerProvider()
    {
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return LfId.IsTypeLfId(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new LfIdSerializer();
    }
}

public class LfIdSerializer : JsonConverter<LfId>
{
    /// <summary>
    /// read json into LfId
    /// </summary>
    public override LfId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var idString = reader.GetString();
        if (idString == null)
        {
            throw new NullReferenceException("LfId can not be null during json deserialization");
        }

        return LfId.FromJson(idString, typeToConvert);
    }

    /// <summary>
    /// write json from LfId
    /// </summary>
    public override void Write(Utf8JsonWriter writer, LfId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetIdForJson());
    }
}