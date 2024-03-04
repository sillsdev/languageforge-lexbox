using LfClassicData.Entities;
using MongoDB.Driver;

namespace LfClassicData;

internal class SystemDbContext
{
    public const string SystemDbName = "scriptureforge";
    private readonly IMongoDatabase _mongoDatabase;

    public SystemDbContext(MongoClient mongoClient)
    {
        _mongoDatabase = mongoClient.GetDatabase(SystemDbName);
        Users = _mongoDatabase.GetCollection<User>("users");
        Projects = _mongoDatabase.GetCollection<LfProject>("projects");
    }

    public IMongoCollection<User> Users { get; }
    public IMongoCollection<LfProject> Projects { get; }
}
