using System.Text.Json;
using System.Text.Json.Serialization;
using LinqToDB.Common;

namespace LcmCrdt.Objects;

[JsonConverter(typeof(DbTranslationDeserializationTargetConverter))]
public class DbTranslationDeserializationTarget
{
    private class DbTranslationDeserializationTargetConverter: JsonConverter<DbTranslationDeserializationTarget>
    {
        public override DbTranslationDeserializationTarget? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var translations = JsonSerializer.Deserialize<IList<Translation>>(ref reader, options);
                if (translations is null) return null;
                return new DbTranslationDeserializationTarget(translations);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var translation = JsonSerializer.Deserialize<RichMultiString>(ref reader, options);
                if (translation.IsNullOrEmpty()) return null;
                return new DbTranslationDeserializationTarget(translation);
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DbTranslationDeserializationTarget value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private readonly IList<Translation> _translations;

    public DbTranslationDeserializationTarget(IList<Translation> translations)
    {
        _translations = translations;
    }

    public DbTranslationDeserializationTarget(RichMultiString translation)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _translations = [Translation.FromMultiString(translation)];
#pragma warning restore CS0618 // Type or member is obsolete
    }

    public IList<Translation> GetTranslations()
    {
        return _translations ?? [];
    }
}
