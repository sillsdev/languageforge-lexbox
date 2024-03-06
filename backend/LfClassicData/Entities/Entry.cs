using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

public class Entry: EntityDocument<Entry>
{
    public Guid Guid { get; set; }
    public required Dictionary<string, LexValue> Lexeme { get; set; }
    public required Dictionary<string, LexValue> Note { get; set; }
    public required Dictionary<string, LexValue> LiteralMeaning { get; set; }
    public required Dictionary<string, LexValue>? CitationForm { get; set; }
    public string? MorphologyType { get; set; }
    public List<Sense?>? Senses { get; set; } = [];
}

[BsonIgnoreExtraElements]
public class Sense
{
    public Guid Guid { get; set; }
    public LexValue? PartOfSpeech { get; set; }
    public LexMultiValue? SemanticDomain { get; set; }

    public required Dictionary<string, LexValue> Gloss { get; set; }
    public required Dictionary<string, LexValue> Definition { get; set; }
    public required Dictionary<string, LexValue>? PhonologyNote { get; set; }
    public List<Example?>? Examples { get; set; } = [];
}

[BsonIgnoreExtraElements]
public class Example
{
    public Guid Guid { get; set; }
    public Dictionary<string, LexValue>? Sentence { get; set; }
    public Dictionary<string, LexValue>? Translation { get; set; }
    public Dictionary<string, LexValue>? Reference { get; set; }

}
