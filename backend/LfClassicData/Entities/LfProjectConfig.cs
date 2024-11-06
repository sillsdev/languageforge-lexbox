using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

public class LfProjectConfig
{
    public required LfEntryConfig Entry { get; init; }
}

public class LfEntryConfig
{
    public required LfFieldsConfig Fields { get; init; }
}

[BsonIgnoreExtraElements]
public class LfFieldsConfig
{
    public required LfFieldConfig? Lexeme { get; init; }
    public required LfFieldConfig? CitationForm { get; init; }

}

[BsonIgnoreExtraElements]
public class LfFieldConfig
{
    public required List<string> InputSystems { get; init; }
}
