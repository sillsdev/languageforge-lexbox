using MiniLcm;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LfClassicData;

public class LfClassicLexboxApi(string projectCode, ProjectDbContext dbContext) : ILexboxApi
{
    private IMongoCollection<Entities.Entry> Entries => dbContext.Entries(projectCode);

    public async Task<WritingSystems> GetWritingSystems()
    {
        return null;
    }

    public async Task<string[]> GetExemplars()
    {
        return new string[] { };
    }

    public async Task<Entry[]> GetEntries(string exemplar, QueryOptions? options = null)
    {
        return new Entry[] { };
    }

    public async Task<Entry[]> GetEntries(QueryOptions? options = null)
    {
        return new Entry[] { };
    }

    public async Task<Entry[]> SearchEntries(string query, QueryOptions? options = null)
    {
        using var entriesCursor = await Entries.Find(Builders<Entities.Entry>.Filter.Empty).ToCursorAsync();
        var entries = new List<Entry>();
        while (await entriesCursor.MoveNextAsync())
        {
            foreach (var entry in entriesCursor.Current)
            {
                entries.Add(ToEntry(entry));
            }
        }

        return entries.ToArray();
    }

    private static Entry ToEntry(Entities.Entry entry)
    {
        throw new NotImplementedException();
        return new Entry
        {
        };
    }

    public async Task<Entry> GetEntry(Guid id)
    {
        return null;
    }

    public Task<Entry> CreateEntry(Entry entry)
    {
        throw new NotSupportedException();
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        throw new NotSupportedException();
    }

    public async Task DeleteEntry(Guid id)
    {
        throw new NotSupportedException();
    }

    public Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        throw new NotSupportedException();
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        throw new NotSupportedException();
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        throw new NotSupportedException();
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        throw new NotSupportedException();
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        throw new NotSupportedException();
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        throw new NotSupportedException();
    }

    public UpdateBuilder<T> CreateUpdateBuilder<T>() where T : class
    {
        throw new NotSupportedException();
    }
}
