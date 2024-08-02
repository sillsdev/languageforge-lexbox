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
        if (inputSystems is null) return new();
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
            //ws type might not be stored, we will add it anyway, otherwise nothing works
            if (inputSystem is { AnalysisWS: null, VernacularWS: null })
            {
                analysis.Add(writingSystem);
                vernacular.Add(writingSystem);
            }
            if (inputSystem.AnalysisWS is true) analysis.Add(writingSystem);
            if (inputSystem.VernacularWS is true) vernacular.Add(writingSystem);
        }
        return new WritingSystems
        {
            Vernacular = vernacular.ToArray(),
            Analysis = analysis.ToArray()
        };
    }

    public async IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        var optionListItems = await dbContext.GetOptionListItems(projectCode, "grammatical-info");

        foreach (var item in optionListItems)
        {
            yield return new PartOfSpeech
            {
                Id = item.Guid ?? Guid.Empty,
                Name = new MultiString { { "en", item.Value ?? item.Abbreviation ?? string.Empty } }
            };
        }
    }

    public async Task CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        throw new NotSupportedException();
    }

    public async Task CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return AsyncEnumerable.Empty<SemanticDomain>();
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        throw new NotSupportedException();
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        throw new NotSupportedException();
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

        var sortWs = options.Order.WritingSystem;
        if (sortWs == "default")
        {
            var ws = await GetWritingSystems();
            if (ws is { Vernacular: [], Analysis: [] })
            {
                yield break;
            }
            sortWs = ws.Vernacular[0].Id;
        }

        await foreach (var entry in Entries.AsQueryable()
                           //todo, you can only sort by headword for now
                           .Select(entry => new {entry, headword = entry.CitationForm![sortWs].Value ?? entry.Lexeme![sortWs].Value ?? string.Empty})
                           .OrderBy(e => e.headword)
                           .ThenBy(e => e.entry.MorphologyType)
                           .ThenBy(e => e.entry.Guid) //todo should sort by homograph number
                           .Skip(options.Offset)
                           .Take(options.Count)
                           .ToAsyncEnumerable()
                           .Select(e => ToEntry(e.entry)))
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
            SemanticDomains = (sense.SemanticDomain?.Values ?? [])
                .Select(sd => new SemanticDomain { Id = Guid.Empty, Code = sd, Name = new MultiString { { "en", sd } } })
                .ToList(),
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
