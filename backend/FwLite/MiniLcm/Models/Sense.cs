using System.Text.Json;
using System.Text.Json.Serialization;
using MiniLcm.Attributes;

namespace MiniLcm.Models;

public class Sense : IObjectWithId<Sense>, IOrderable
{
    public virtual Guid Id { get; set; }
    [MiniLcmInternal]
    public double Order { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid EntryId { get; set; }
    public virtual RichMultiString Definition { get; set; } = new();
    public virtual MultiString Gloss { get; set; } = new();

    [JsonConverter(typeof(SensePoSConverter))]
    public virtual PartOfSpeech? PartOfSpeech { get; set; } = null;
    public virtual Guid? PartOfSpeechId { get; set; }
    public virtual IList<SemanticDomain> SemanticDomains { get; set; } = [];
    public virtual List<ExampleSentence> ExampleSentences { get; set; } = [];

    public Guid[] GetReferences()
    {
        ReadOnlySpan<Guid> pos = PartOfSpeechId.HasValue ? [PartOfSpeechId.Value] : [];
        return [EntryId, ..pos, ..SemanticDomains.Select(sd => sd.Id)];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        if (id == EntryId)
            DeletedAt = time;
        if (id == PartOfSpeechId)
        {
            PartOfSpeechId = null;
            PartOfSpeech = null;
        }
        SemanticDomains = [..SemanticDomains.Where(sd => sd.Id != id)];
    }

    public Sense Copy()
    {
        return new Sense
        {
            Id = Id,
            EntryId = EntryId,
            Order = Order,
            DeletedAt = DeletedAt,
            Definition = Definition.Copy(),
            Gloss = Gloss.Copy(),
            PartOfSpeech = PartOfSpeech?.Copy(),
            PartOfSpeechId = PartOfSpeechId,
            SemanticDomains = [..SemanticDomains.Select(s => s.Copy())],
            ExampleSentences = [..ExampleSentences.Select(s => s.Copy())]
        };
    }
}

internal class SensePoSConverter : JsonConverter<PartOfSpeech?>
{
    public override PartOfSpeech? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //PartOfSpeech used to be a string, we can just leave it as null if it's a string since the PartOfSpeechId is what we really care about
        if (reader.TokenType == JsonTokenType.String) return null;
        return JsonSerializer.Deserialize<PartOfSpeech>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, PartOfSpeech? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
