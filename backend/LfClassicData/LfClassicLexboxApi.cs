using LfClassicData.Entities;
using MiniLcm;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Entry = MiniLcm.Entry;
using Sense = MiniLcm.Sense;

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
        return await Query();
    }

    public async Task<Entry[]> GetEntries(QueryOptions? options = null)
    {
        return await Query();
    }

    public async Task<Entry[]> SearchEntries(string query, QueryOptions? options = null)
    {
        return await Query();
    }

    private async Task<Entry[]> Query()
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
        return new Entry
        {
            Id = entry.Guid,
            CitationForm = ToMultiString(entry.CitationForm),
            LexemeForm = ToMultiString(entry.Lexeme),
            Note = new(),//todo add note
            LiteralMeaning = new(),//todo add meaning
            Senses = entry.Senses?.OfType<Entities.Sense>().Select(ToSense).ToList() ?? []
        };
    }

    private static Sense ToSense(Entities.Sense sense)
    {
        return new Sense
        {
            Id = sense.Guid,
            Gloss = ToMultiString(sense.Gloss),
            PartOfSpeech = sense.PartOfSpeech?.Value ?? string.Empty,
            ExampleSentences = sense.Examples?.OfType<Example>().Select(ToExampleSentence).ToList() ?? []
        };
    }

    private static ExampleSentence ToExampleSentence(Example example)
    {
        return new ExampleSentence
        {
            Id = example.Guid,
        };
    }

    private static MultiString ToMultiString(Dictionary<string, MultiTextValue>? multiTextValue)
    {
        var ms = new MultiString();
        if (multiTextValue is null) return ms;
        foreach (var (key, value) in multiTextValue)
        {
            ms.Values[key] = value.Value;
        }

        return ms;
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
