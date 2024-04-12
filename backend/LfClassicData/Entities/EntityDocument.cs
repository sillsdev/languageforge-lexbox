using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

public class EntityDocument<T> : EntityBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; init; }
}
