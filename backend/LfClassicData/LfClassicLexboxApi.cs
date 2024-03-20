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

    public IAsyncEnumerable<Entry> GetEntries(string exemplar, QueryOptions? options = null)
    {
        return Query();
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        options ??= QueryOptions.Default;
        return Query()
            .Skip(options.Offset)
            .Take(options.Count);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        options ??= QueryOptions.Default;
        return Query()
            .Where(e => e.MatchesQuery(query))
            .Skip(options.Offset)
            .Take(options.Count);
    }

    private async IAsyncEnumerable<Entry> Query()
    {
        using var entriesCursor = await Entries.Find(Builders<Entities.Entry>.Filter.Empty).ToCursorAsync();
        while (await entriesCursor.MoveNextAsync())
        {
            foreach (var entry in entriesCursor.Current)
            {
                yield return ToEntry(entry);
            }
        }
    }

    private static Entry ToEntry(Entities.Entry entry)
    {
        return new Entry
        {
            Id = entry.Guid,
            CitationForm = ToMultiString(entry.CitationForm),
            LexemeForm = ToMultiString(entry.Lexeme),
            Note = ToMultiString(entry.Note),
            LiteralMeaning = ToMultiString(entry.LiteralMeaning),
            Senses = entry.Senses?.OfType<Entities.Sense>().Select(ToSense).ToList() ?? [],
        };
    }

    private static Sense ToSense(Entities.Sense sense)
    {
        return new Sense
        {
            Id = sense.Guid,
            Gloss = ToMultiString(sense.Gloss),
            Definition = ToMultiString(sense.Definition),
            PartOfSpeech = sense.PartOfSpeech?.Value ?? string.Empty,
            SemanticDomain = sense.SemanticDomain?.Values ?? [],
            ExampleSentences = sense.Examples?.OfType<Example>().Select(ToExampleSentence).ToList() ?? [],
        };
    }

    private static ExampleSentence ToExampleSentence(Example example)
    {
        return new ExampleSentence
        {
            Id = example.Guid,
            Reference = (example.Reference?.TryGetValue("en", out var value) == true) ? value.Value : string.Empty,
            Sentence = ToMultiString(example.Sentence),
            Translation = ToMultiString(example.Translation)
        };
    }

    private static MultiString ToMultiString(Dictionary<string, LexValue>? multiTextValue)
    {
        var ms = new MultiString();
        if (multiTextValue is null) return ms;
        foreach (var (key, value) in multiTextValue)
        {
            ms.Values[key] = value.Value;
        }

        return ms;
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        var entry = await Entries.Find(e => e.Guid == id).FirstOrDefaultAsync();
        if (entry is null) return null;
        return ToEntry(entry);
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
