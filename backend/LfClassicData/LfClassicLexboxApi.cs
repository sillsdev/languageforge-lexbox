using LfClassicData.Entities;
using MiniLcm;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Entry = MiniLcm.Entry;
using Sense = MiniLcm.Sense;

namespace LfClassicData;

public class LfClassicLexboxApi(string projectCode, ProjectDbContext dbContext, SystemDbContext systemDbContext) : ILexboxApi
{
    private IMongoCollection<Entities.Entry> Entries => dbContext.Entries(projectCode);

    public async Task<WritingSystems> GetWritingSystems()
    {
        var inputSystems = await systemDbContext.Projects.AsQueryable()
            .Where(p => p.ProjectCode == projectCode)
            .Select(p => p.InputSystems)
            .FirstOrDefaultAsync();
        var vernacular = new List<WritingSystem>();
        var analysis = new List<WritingSystem>();
        foreach (var (ws, inputSystem) in inputSystems)
        {
            var writingSystem = new WritingSystem
            {
                Id = ws,
                Font = "???",
                Name = inputSystem.LanguageName,
                Abbreviation = inputSystem.Abbreviation
            };
            if (inputSystem.AnalysisWS) analysis.Add(writingSystem);
            if (inputSystem.VernacularWS) vernacular.Add(writingSystem);
        }
        return new WritingSystems
        {
            Vernacular = vernacular.ToArray(),
            Analysis = analysis.ToArray()
        };
    }

    public IAsyncEnumerable<Entry> GetEntries(string exemplar, QueryOptions? options = null)
    {
        return Query();
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return Query(options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return Query(options)
            .Where(e => e.MatchesQuery(query));
    }

    private async IAsyncEnumerable<Entry> Query(QueryOptions? options = null)
    {
        options ??= QueryOptions.Default;
        var ws = await GetWritingSystems();
        var sortWs = options.Order.WritingSystem;
        if (sortWs == "default")
        {
            sortWs = ws.Vernacular[0].Id;
        }

        await foreach (var entry in Entries.ToAsyncEnumerable()
                           //todo, you can only sort by headword for now
                           .OrderBy(e => e.CitationForm?.TryGetValue(sortWs, out var val) == true
                               ? val.Value
                               : e.Lexeme.TryGetValue(sortWs, out val)
                                   ? val.Value
                                   : string.Empty)
                           .Skip(options.Offset)
                           .Take(options.Count)
                           .Select(ToEntry))
        {
            yield return entry;
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
