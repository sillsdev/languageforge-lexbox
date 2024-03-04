using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

public class EntityDocument<T> : EntityBase
{
    [BsonId]
    public required string Id { get; init; }
}
