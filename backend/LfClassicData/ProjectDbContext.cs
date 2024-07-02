using LfClassicData.Entities;
using MongoDB.Driver;

namespace LfClassicData;

public class ProjectDbContext
{
    private readonly MongoClient _mongoClient;

    public ProjectDbContext(MongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    private IMongoCollection<T> GetCollection<T>(string projectCode, string collectionName)
    {
        return _mongoClient.GetDatabase($"sf_{projectCode}").GetCollection<T>(collectionName);
    }

    public IMongoCollection<Entry> Entries(string projectCode)
    {
        return GetCollection<Entry>(projectCode, "lexicon");
    }

    public async Task<OptionListItem[]> GetOptionListItems(string projectCode, string listCode)
    {
        var collection = GetCollection<OptionListRecord>(projectCode, "optionlists");
        var result = await collection.Find(e => e.Code == listCode).FirstOrDefaultAsync();
        if (result is null) return [];
        return [..result.Items];
    }
}
