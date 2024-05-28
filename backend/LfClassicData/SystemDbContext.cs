using LfClassicData.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LfClassicData;

public class SystemDbContext
{
    public const string SystemDbName = "scriptureforge";
    private readonly IMongoDatabase _mongoDatabase;
    private bool? _isAvailable;

    public SystemDbContext(MongoClient mongoClient)
    {
        _mongoDatabase = mongoClient.GetDatabase(SystemDbName);
        Users = _mongoDatabase.GetCollection<User>("users");
        Projects = _mongoDatabase.GetCollection<LfProject>("projects");
    }

    internal IMongoCollection<User> Users { get; }
    public IMongoCollection<LfProject> Projects { get; }

    public async Task<bool> IsAvailable()
    {
        if (_isAvailable is null or false)
        {
            _isAvailable = await Task.Run(() =>
            {
                return _mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(100);
            });
        }
        return _isAvailable.Value;
    }
}
