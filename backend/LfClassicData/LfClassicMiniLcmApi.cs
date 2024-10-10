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

    public async Task<string?> PickDefaultVernacularWritingSystem()
    {
        var cacheKey = $"DefaultVernacular";
        if (memoryCache.TryGetValue(cacheKey, out string? cachedWs)) return cachedWs!;

        var fieldConfigs = await systemDbContext.Projects.AsQueryable()
            .Where(p => p.ProjectCode == projectCode)
            .Select(p => p.Config.Entry.Fields)
            .FirstOrDefaultAsync();

        var ws = fieldConfigs.CitationForm?.InputSystems.FirstOrDefault(ws => !ws.Contains("-audio"))
            ?? fieldConfigs.Lexeme?.InputSystems.FirstOrDefault(ws => !ws.Contains("-audio"));
        if (ws is not null or "") memoryCache.Set(cacheKey, ws, TimeSpan.FromHours(1));
        return ws;
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

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return AsyncEnumerable.Empty<SemanticDomain>();
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
            yield return ToEntry(entry);
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
                        { "then", $"$citationForm.{sortWs}.value" },
                        { "else", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$and", new BsonArray
                                    {
                                        new BsonDocument("$ne", new BsonArray { new BsonDocument("$type", $"$lexeme.{sortWs}.value"), "missing" }),
                                        new BsonDocument("$ne", new BsonArray { $"$lexeme.{sortWs}.value", BsonNull.Value }),
                                        new BsonDocument("$ne", new BsonArray { new BsonDocument("$trim", new BsonDocument("input", $"$lexeme.{sortWs}.value")), "" }),
                                    })
                                },
                                { "then", $"$lexeme.{sortWs}.value" },
                                { "else", "" }
                            })
                        }
                    })
                }
            })));
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
}
