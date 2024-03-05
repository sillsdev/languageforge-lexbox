using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace LfClassicData.Configuration;

public static class BsonConfiguration
{
    public static void Setup()
    {
        var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("LF Conventions", conventionPack, _ => true);
    }
}
