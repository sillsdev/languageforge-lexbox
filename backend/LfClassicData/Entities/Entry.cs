using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

public class Entry: EntityDocument<Entry>
{
    public Guid Guid { get; set; }
    public required Dictionary<string, MultiTextValue> Lexeme { get; set; }
    public required Dictionary<string, MultiTextValue>? CitationForm { get; set; }
    public string? MorphologyType { get; set; }
    public List<Sense?>? Senses { get; set; } = [];
}

[BsonIgnoreExtraElements]
public class Sense
{
    public Guid Guid { get; set; }
    public MultiTextValue? PartOfSpeech { get; set; }
    public required Dictionary<string, MultiTextValue> Gloss { get; set; }
    public required Dictionary<string, MultiTextValue>? PhonologyNote { get; set; }
    public List<Example?>? Examples { get; set; } = [];
}

[BsonIgnoreExtraElements]
public class Example
{
    public Guid Guid { get; set; }
    public Dictionary<string, MultiTextValue>? Reference { get; set; }

}
