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
        if (_isAvailable is null)
        {
            try
            {
                //lang=json
                await _mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}")
                    .WaitAsync(TimeSpan.FromSeconds(3));
                _isAvailable = true;
            }
            catch
            {
                _isAvailable = false;
            }
        }
        return _isAvailable.Value;
    }
}
