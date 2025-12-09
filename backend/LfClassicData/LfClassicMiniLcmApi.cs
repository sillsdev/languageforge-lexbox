using System.Text.RegularExpressions;
using LfClassicData.Entities;
using LfClassicData.Entities.MongoUtils;
using Microsoft.Extensions.Caching.Memory;
using MiniLcm;
using MiniLcm.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Entry = MiniLcm.Models.Entry;
using Sense = MiniLcm.Models.Sense;

namespace LfClassicData;

public class LfClassicMiniLcmApi(string projectCode, ProjectDbContext dbContext, SystemDbContext systemDbContext, IMemoryCache memoryCache) : IMiniLcmReadApi
{
    private IMongoCollection<Entities.Entry> Entries => dbContext.Entries(projectCode);
    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return AsyncEnumerable.Empty<ComplexFormType>();
    }

    public Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return Task.FromResult<ComplexFormType?>(null);
    }

    public IAsyncEnumerable<MorphTypeData> GetAllMorphTypeData()
    {
        return AsyncEnumerable.Empty<MorphTypeData>();
    }

    public Task<MorphTypeData?> GetMorphTypeData(Guid id)
    {
        return Task.FromResult<MorphTypeData?>(null);
    }

    private Dictionary<Guid, PartOfSpeech>? _partsOfSpeechCacheByGuid = null;
    private Dictionary<string, PartOfSpeech>? _partsOfSpeechCacheByStringKey = null;

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
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = ws,
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

    public async Task<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        var ws = await GetWritingSystems();
        return type switch
        {
            WritingSystemType.Vernacular => ws.Vernacular.FirstOrDefault(w => w.WsId == id),
            WritingSystemType.Analysis => ws.Analysis.FirstOrDefault(w => w.WsId == id),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public async Task<string?> PickDefaultVernacularWritingSystem()
    {
        var cacheKey = $"LfClassic|DefaultVernacular|{projectCode}";
        if (memoryCache.TryGetValue(cacheKey, out string? cachedWs)) return cachedWs!;

        var fieldConfigs = await systemDbContext.Projects.AsQueryable()
            .Where(p => p.ProjectCode == projectCode)
            .Select(p => p.Config.Entry.Fields)
            .FirstOrDefaultAsync();

        var ws = fieldConfigs.CitationForm?.InputSystems.FirstOrDefault(ws => !ws.Contains("-audio"))
            ?? fieldConfigs.Lexeme?.InputSystems.FirstOrDefault(ws => !ws.Contains("-audio"));
        if (!string.IsNullOrEmpty(ws)) memoryCache.Set(cacheKey, ws, TimeSpan.FromHours(1));
        return ws;
    }

    public async IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        var optionListItems = await dbContext.GetOptionListItems(projectCode, "grammatical-info");

        foreach (var item in optionListItems)
        {
            yield return ToPartOfSpeech(item);
        }
    }

    public IAsyncEnumerable<Publication> GetPublications()
    {
        return AsyncEnumerable.Empty<Publication>();
    }

    public async Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        if (_partsOfSpeechCacheByGuid is null)
        {
            _partsOfSpeechCacheByGuid = await GetPartsOfSpeech().ToDictionaryAsync(pos => pos.Id);
        }
        return _partsOfSpeechCacheByGuid.GetValueOrDefault(id);
    }

    public Task<Publication?> GetPublication(Guid id)
    {
        return Task.FromResult<Publication?>(null);
    }

    public async ValueTask<PartOfSpeech?> GetPartOfSpeech(string key)
    {
        if (_partsOfSpeechCacheByStringKey is null)
        {
            _partsOfSpeechCacheByStringKey = await GetPartsOfSpeech().ToDictionaryAsync(pos => pos.Name["__key"]);
        }
        return _partsOfSpeechCacheByStringKey.GetValueOrDefault(key);
    }

    public async IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        var optionListItems = await dbContext.GetOptionListItems(projectCode, "semantic-domain-ddp4");

        foreach (var item in optionListItems)
        {
            yield return ToSemanticDomain(item);
        }
    }

    public async Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return await GetSemanticDomains().FirstOrDefaultAsync(semdom => semdom.Id == id);
    }

    public async Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        // not efficient, but this will likely never get used
        var entries = Query(new QueryOptions
        {
            Count = QueryOptions.QueryAll,
            Exemplar = options?.Exemplar,
            Filter = options?.Filter
        }, query);
        return await entries.CountAsync();
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return Query(options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return Query(options, query);
    }

    private async IAsyncEnumerable<Entry> Query(QueryOptions? options = null, string? search = null)
    {
        options ??= QueryOptions.Default;

        var sortWs = options.Order.WritingSystem;
        if (sortWs == "default")
        {
            var defaultWs = await PickDefaultVernacularWritingSystem();
            if (defaultWs is null) yield break;
            sortWs = defaultWs;
        }
        else if (!Regex.IsMatch(sortWs, "^[\\d\\w-]+$"))
        {
            // We sort of inject this into our Mongo queries, so some validation seems reasonable
            throw new ArgumentException($"Invalid writing system {sortWs}", "options.Order.WritingSystem");
        }

        if (options.Order.Field != SortField.Headword)
        {
            throw new NotSupportedException($"Sorting by {options.Order.Field} is not supported");
        }

        PipelineDefinition<Entities.Entry, Entities.Entry> pipeline = new EmptyPipelineDefinition<Entities.Entry>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            pipeline = AppendSearchAndFilter(search, pipeline);
        }

        pipeline = AppendHeadWord(sortWs, pipeline)
            .Sort(Builders<HeadwordEntry>.Sort
                .Ascending(entry => entry.headword)
                .Ascending(entry => entry.MorphologyType)
                .Ascending(entry => entry.Guid))
            .Skip(options.Offset)
            .Limit(options.Count)
            .Project(entry => entry as Entities.Entry);

        await foreach (var entry in Entries.Aggregate(pipeline).ToAsyncEnumerable())
        {
            yield return await ToEntry(entry);
        }
    }

    private PipelineDefinition<Entities.Entry, Entities.Entry> AppendSearchAndFilter(string search,
        PipelineDefinition<Entities.Entry, Entities.Entry> pipeline)
    {
        var emptyDoc = new BsonDocument();
        var searchLower = Regex.Escape(search.ToLower());
        return pipeline.AppendStage(new BsonDocumentPipelineStageDefinition<Entities.Entry, Entities.Entry>(
                    new BsonDocument("$addFields", new BsonDocument
                    {
                        { "searchFields", new BsonDocument("$concatArrays", new BsonArray
                            {
                                new BsonDocument("$objectToArray", new BsonDocument("$ifNull", new BsonArray { "$lexeme", emptyDoc })),
                                new BsonDocument("$objectToArray", new BsonDocument("$ifNull", new BsonArray { "$citationForm", emptyDoc })),
                                new BsonDocument("$reduce", new BsonDocument
                                {
                                    { "input", new BsonDocument("$ifNull", new BsonArray { "$senses", emptyDoc })},
                                    { "initialValue", new BsonArray() },
                                    { "in", new BsonDocument("$concatArrays", new BsonArray
                                        {
                                            "$$value",
                                            new BsonDocument("$objectToArray", new BsonDocument("$ifNull", new BsonArray { "$$this.gloss", emptyDoc }))
                                        })
                                    }
                                })
                            })
                        }
                    })))
                .AppendStage(new BsonDocumentPipelineStageDefinition<Entities.Entry, Entities.Entry>(
                    new BsonDocument("$match",
                        new BsonDocument("searchFields", new BsonDocument("$elemMatch", new BsonDocument("v.value", new BsonRegularExpression(searchLower, "i"))))
                    )
                ));
    }

    private PipelineDefinition<Entities.Entry, HeadwordEntry> AppendHeadWord(WritingSystemId sortWs,
        PipelineDefinition<Entities.Entry, Entities.Entry> pipeline)
    {
        //this effectively does:
        //list.map(e => ({...e, headword: e.citationForm[sortWs].value ?? e.lexeme[sortWs].value ?? ''}))
        return pipeline.AppendStage(new BsonDocumentPipelineStageDefinition<Entities.Entry, HeadwordEntry>(
            new BsonDocument("$addFields", new BsonDocument
            {
                { nameof(HeadwordEntry.headword).ToLower(), new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$and", new BsonArray
                            {
                                new BsonDocument("$ne", new BsonArray { new BsonDocument("$type", $"$citationForm.{sortWs}.value"), "missing" }),
                                new BsonDocument("$ne", new BsonArray { $"$citationForm.{sortWs}.value", BsonNull.Value }),
                                new BsonDocument("$ne", new BsonArray { new BsonDocument("$trim", new BsonDocument("input", $"$citationForm.{sortWs}.value")), "" }),
                            })
                        },
                        { "then", new BsonDocument("$toLower", $"$citationForm.{sortWs}.value") },
                        { "else", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$and", new BsonArray
                                    {
                                        new BsonDocument("$ne", new BsonArray { new BsonDocument("$type", $"$lexeme.{sortWs}.value"), "missing" }),
                                        new BsonDocument("$ne", new BsonArray { $"$lexeme.{sortWs}.value", BsonNull.Value }),
                                        new BsonDocument("$ne", new BsonArray { new BsonDocument("$trim", new BsonDocument("input", $"$lexeme.{sortWs}.value")), "" }),
                                    })
                                },
                                { "then", new BsonDocument("$toLower", $"$lexeme.{sortWs}.value") },
                                { "else", "" }
                            })
                        }
                    })
                }
            })));
    }

    private async ValueTask<Entry> ToEntry(Entities.Entry entry)
    {
        List<Sense> senses = new(entry.Senses?.Count ?? 0);
        if (entry.Senses is not (null or []))
        {
            foreach (var sense in entry.Senses)
            {
                if (sense is null) continue;
                //explicitly doing this sequentially
                //to avoid concurrency issues as ToSense calls GetPartOfSpeech which is cached
                senses.Add(await ToSense(entry.Guid, sense));
            }
        }

        return new Entry
        {
            Id = entry.Guid,
            CitationForm = ToMultiString(entry.CitationForm),
            LexemeForm = ToMultiString(entry.Lexeme),
            Note = ToRichMultiString(entry.Note),
            LiteralMeaning = ToRichMultiString(entry.LiteralMeaning),
            Senses = new List<Sense>(senses),
        };
    }

    private async ValueTask<Sense> ToSense(Guid entryId, Entities.Sense sense)
    {
        var partOfSpeech = sense.PartOfSpeech is null ? null : await GetPartOfSpeech(sense.PartOfSpeech.Value);
        return new Sense
        {
            Id = sense.Guid,
            EntryId = entryId,
            Gloss = ToMultiString(sense.Gloss),
            Definition = ToRichMultiString(sense.Definition),
            PartOfSpeech = partOfSpeech,
            PartOfSpeechId = partOfSpeech?.Id,
            SemanticDomains = (sense.SemanticDomain?.Values ?? [])
                .Select(sd => new SemanticDomain { Id = Guid.Empty, Code = sd, Name = new MultiString { { "en", sd } } })
                .ToList(),
            ExampleSentences = sense.Examples?.OfType<Example>().Select(example => ToExampleSentence(sense.Guid, example)).ToList() ?? [],
        };
    }

    private static ExampleSentence ToExampleSentence(Guid senseId, Example example)
    {
        return new ExampleSentence
        {
            Id = example.Guid,
            SenseId = senseId,
            Reference = new((example.Reference?.TryGetValue("en", out var value) == true) ? value.Value : string.Empty),
            Sentence = ToRichMultiString(example.Sentence),
            Translations = [new Translation() { Id = Guid.NewGuid(), Text = ToRichMultiString(example.Translation) }]
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

    private static RichMultiString ToRichMultiString(Dictionary<string, LexValue>? multiTextValue)
    {
        var ms = new RichMultiString();
        if (multiTextValue is null) return ms;
        foreach (var (key, value) in multiTextValue)
        {
            ms[key] = new RichString(value.Value);
        }

        return ms;
    }

    private static PartOfSpeech ToPartOfSpeech(Entities.OptionListItem item)
    {
        return new PartOfSpeech
        {
            Id = item.Guid ?? Guid.Empty,
            Name = new MultiString
            {
                { "en", item.Value ?? item.Abbreviation ?? string.Empty },
                { "__key", item.Key ?? string.Empty } // The key is all that senses have on them, so we need it client-side to find the display name
            },
            // TODO: Abbreviation
            Predefined = false,
        };
    }

    private static SemanticDomain ToSemanticDomain(Entities.OptionListItem item)
    {
        // TODO: Needs testing against actual LF testlangproj data
        return new SemanticDomain
        {
            Id = item.Guid ?? Guid.Empty,
            Name = new MultiString
            {
                { "en", item.Value ?? item.Abbreviation ?? string.Empty },
                { "__key", item.Key ?? string.Empty } // The key is all that senses have on them, so we need it client-side to find the display name
            },
            Predefined = false,
        };
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        var entry = await Entries.Find(e => e.Guid == id).FirstOrDefaultAsync();
        if (entry is null) return null;
        return await ToEntry(entry);
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid id)
    {
        var entry = await Entries.Find(e => e.Guid == entryId).FirstOrDefaultAsync();
        if (entry is null) return null;
        var sense = entry.Senses?.FirstOrDefault(s => s?.Guid == id);
        if (sense is null) return null;
        return await ToSense(entryId, sense);
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        var entry = await Entries.Find(e => e.Guid == entryId).FirstOrDefaultAsync();
        if (entry is null) return null;
        var sense = entry.Senses?.FirstOrDefault(s => s?.Guid == senseId);
        if (sense is null) return null;
        var exampleSentence = sense.Examples?.FirstOrDefault(e => e?.Guid == id);
        if (exampleSentence is null) return null;
        return ToExampleSentence(sense.Guid, exampleSentence);
    }

    public async Task<EntryWindowResponse> GetEntriesWindow(int start, int size, string? query = null, QueryOptions? options = null)
    {
        var entries = new List<Entry>();
        await foreach (var entry in GetEntries(options))
        {
            entries.Add(entry);
        }
        // Apply manual paging after getting all entries (not optimal but compatible with Lfclassic)
        var pagedEntries = entries.Skip(start).Take(size).ToList();
        return new EntryWindowResponse(pagedEntries, start);
    }

    public async Task<EntryRowIndexResponse> GetEntryRowIndex(Guid entryId, string? query = null, QueryOptions? options = null)
    {
        var rowIndex = 0;
        await foreach (var entry in GetEntries(options))
        {
            if (entry.Id == entryId)
            {
                var fullEntry = await GetEntry(entryId);
                if (fullEntry is null)
                    throw new KeyNotFoundException($"Entry {entryId} not found");
                return new EntryRowIndexResponse(rowIndex, fullEntry);
            }
            rowIndex++;
        }
        throw new KeyNotFoundException($"Entry {entryId} not found");
    }
}
