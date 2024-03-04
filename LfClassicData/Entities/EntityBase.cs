using MongoDB.Bson.Serialization.Attributes;

namespace LfClassicData.Entities;

[BsonIgnoreExtraElements(Inherited = true)]
public abstract class EntityBase
{
}
