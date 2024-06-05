using System.Linq.Expressions;
using System.Text.Json;
using Crdt.Core;
using Crdt;
using Crdt.Changes;
using LcmCrdt.Changes;
using MiniLcm;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;

namespace LcmCrdt;

public class CrdtLexboxApi(DataModel dataModel, JsonSerializerOptions jsonOptions, IHybridDateTimeProvider timeProvider, CurrentProjectService projectService) : ILexboxApi
{
    private async ValueTask<Guid> GetClientId()
    {
        return (await projectService.GetProjectData()).ClientId;
    }


    private IQueryable<Entry> Entries => dataModel.GetLatestObjects<Entry>();
    private IQueryable<Sense> Senses => dataModel.GetLatestObjects<Sense>();
    private IQueryable<ExampleSentence> ExampleSentences => dataModel.GetLatestObjects<ExampleSentence>();
    private IQueryable<WritingSystem> WritingSystems => dataModel.GetLatestObjects<WritingSystem>();

    public async Task<WritingSystems> GetWritingSystems()
    {
        var systems = await WritingSystems.ToArrayAsync();
        return new WritingSystems
        {
            Analysis = systems.Where(ws => ws.Type == WritingSystemType.Analysis)
                .Select(w => ((MiniLcm.WritingSystem)w)).ToArray(),
            Vernacular = systems.Where(ws => ws.Type == WritingSystemType.Vernacular)
                .Select(w => ((MiniLcm.WritingSystem)w)).ToArray()
        };
    }

    public async Task<MiniLcm.WritingSystem> CreateWritingSystem(WritingSystemType type, MiniLcm.WritingSystem writingSystem)
    {
        var entityId = Guid.NewGuid();
        var wsCount = await WritingSystems.CountAsync(ws => ws.Type == type);
        await dataModel.AddChange(await GetClientId(), new CreateWritingSystemChange(writingSystem, type, entityId, wsCount));
        return await dataModel.GetLatest<WritingSystem>(entityId) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<MiniLcm.WritingSystem> update)
    {
        var ws = await GetWritingSystem(id, type);
        if (ws is null) throw new NullReferenceException($"unable to find writing system with id {id}");
        var patchChange = new JsonPatchChange<WritingSystem>(ws.Id, update.Patch, jsonOptions);
        await dataModel.AddChange(await GetClientId(), patchChange);
        return await dataModel.GetLatest<WritingSystem>(ws.Id) ?? throw new NullReferenceException();
    }

    private WritingSystem? _defaultVernacularWs;
    private WritingSystem? _defaultAnalysisWs;
    private async Task<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        if (id == "default")
        {
            return type switch
            {
                WritingSystemType.Analysis => _defaultAnalysisWs ??= await WritingSystems.FirstOrDefaultAsync(ws => ws.Type == type),
                WritingSystemType.Vernacular => _defaultVernacularWs ??= await WritingSystems.FirstOrDefaultAsync(ws => ws.Type == type),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
        return await WritingSystems.FirstOrDefaultAsync(ws => ws.WsId == id && ws.Type == type);
    }

    public IAsyncEnumerable<MiniLcm.Entry> GetEntries(QueryOptions? options = null)
    {
        return GetEntriesAsyncEnum(predicate: null, options);
    }

    public IAsyncEnumerable<MiniLcm.Entry> SearchEntries(string? query, QueryOptions? options = null)
    {
        if (string.IsNullOrEmpty(query)) return GetEntriesAsyncEnum(null, options);
        return GetEntriesAsyncEnum(e => e.LexemeForm.SearchValue(query) || e.CitationForm.SearchValue(query), options);
    }

    private async IAsyncEnumerable<MiniLcm.Entry> GetEntriesAsyncEnum(
        Expression<Func<Entry, bool>>? predicate = null,
        QueryOptions? options = null)
    {
        var entries = await GetEntries(predicate, options);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    private async Task<MiniLcm.Entry[]> GetEntries(
        Expression<Func<Entry, bool>>? predicate = null,
        QueryOptions? options = null)
    {
        options ??= QueryOptions.Default;
        //todo filter on exemplar options and limit results, and sort
        var queryable = Entries;
        if (predicate is not null) queryable = queryable.Where(predicate);
        if (options.Exemplar is not null)
        {
            var ws = (await GetWritingSystem(options.Exemplar.WritingSystem, WritingSystemType.Analysis))?.WsId;
            if (ws is null)
                throw new NullReferenceException($"writing system {options.Exemplar.WritingSystem} not found");
            queryable = queryable.Where(e => e.Headword(ws.Value).StartsWith(options.Exemplar.Value));
        }

        var sortWs = (await GetWritingSystem(options.Order.WritingSystem, WritingSystemType.Analysis))?.WsId;
        if (sortWs is null)
            throw new NullReferenceException($"sort writing system {options.Order.WritingSystem} not found");
        queryable = queryable.OrderBy(e => e.Headword(sortWs.Value))
            // .ThenBy(e => e.Id)
            .Skip(options.Offset)
            .Take(options.Count);
        var entries = await queryable.ToArrayAsyncLinqToDB();
        await LoadSenses(entries);

        return entries;
    }

    private async Task LoadSenses(Entry[] entries)
    {
        var allSenses = (await Senses
                .Where(s => entries.Select(e => e.Id).Contains(s.EntryId))
                .ToArrayAsyncEF())
            .ToLookup(s => s.EntryId)
            .ToDictionary(g => g.Key, g => g.ToArray());
        var allSenseIds = allSenses.Values.SelectMany(s => s, (_, sense) => sense.Id);
        var allExampleSentences = (await ExampleSentences
                .Where(e => allSenseIds.Contains(e.SenseId))
                .ToArrayAsyncEF())
            .ToLookup(s => s.SenseId)
            .ToDictionary(g => g.Key, g => g.ToArray());
        foreach (var entry in entries)
        {
            entry.Senses = allSenses.TryGetValue(entry.Id, out var senses) ? senses.ToArray() : [];
            foreach (var sense in entry.Senses)
            {
                sense.ExampleSentences = allExampleSentences.TryGetValue(sense.Id, out var sentences)
                    ? sentences.ToArray()
                    : [];
            }
        }
    }

    public async Task<MiniLcm.Entry?> GetEntry(Guid id)
    {
        var entry = await Entries.SingleOrDefaultAsync(e => e.Id == id);
        if (entry is null) return null;
        var senses = await Senses
                .Where(s => s.EntryId == id).ToArrayAsyncLinqToDB();
        var exampleSentences = (await ExampleSentences
                .Where(e => senses.Select(s => s.Id).Contains(e.SenseId)).ToArrayAsyncEF())
            .ToLookup(e => e.SenseId)
            .ToDictionary(g => g.Key, g => g.ToArray());
        entry.Senses = senses;
        foreach (var sense in senses)
        {
            sense.ExampleSentences = exampleSentences.TryGetValue(sense.Id, out var sentences) ? sentences.ToArray() : [];
        }

        return entry;
    }

    /// <summary>
    /// does not return the newly created entry, used for importing a large amount of data
    /// </summary>
    /// <param name="entry"></param>
    public async Task CreateEntryLite(MiniLcm.Entry entry)
    {
        await dataModel.AddChanges(await GetClientId(),
        [
            new CreateEntryChange(entry),
            ..entry.Senses.Select(s => new CreateSenseChange(s, entry.Id)),
            ..entry.Senses.SelectMany(s => s.ExampleSentences,
                (sense, sentence) => new CreateExampleSentenceChange(sentence, sense.Id))
        ], deferCommit: true);
    }

    public async Task<MiniLcm.Entry> CreateEntry(MiniLcm.Entry entry)
    {
        await dataModel.AddChanges(await GetClientId(),
        [
            new CreateEntryChange(entry),
            ..entry.Senses.Select(s => new CreateSenseChange(s, entry.Id)),
            ..entry.Senses.SelectMany(s => s.ExampleSentences,
                (sense, sentence) => new CreateExampleSentenceChange(sentence, sense.Id))
        ]);
        return await GetEntry(entry.Id) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.Entry> UpdateEntry(Guid id,
        UpdateObjectInput<MiniLcm.Entry> update)
    {
        var patchChange = new JsonPatchChange<Entry>(id, update.Patch, jsonOptions);
        await dataModel.AddChange(await GetClientId(), patchChange);
        return await GetEntry(id) ?? throw new NullReferenceException();
    }

    public async Task DeleteEntry(Guid id)
    {
        await dataModel.AddChange(await GetClientId(), new DeleteChange<Entry>(id));
    }

    public async Task<MiniLcm.Sense> CreateSense(Guid entryId, MiniLcm.Sense sense)
    {
        await dataModel.AddChanges(await GetClientId(),
        [
            new CreateSenseChange(sense, entryId),
            ..sense.ExampleSentences.Select(sentence =>
                new CreateExampleSentenceChange(sentence, sense.Id))
        ]);
        return await dataModel.GetLatest<Sense>(sense.Id) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<MiniLcm.Sense> update)
    {
        var patchChange = new JsonPatchChange<Sense>(senseId, update.Patch, jsonOptions);
        await dataModel.AddChange(await GetClientId(), patchChange);
        return await dataModel.GetLatest<Sense>(senseId) ?? throw new NullReferenceException();
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await dataModel.AddChange(await GetClientId(), new DeleteChange<Sense>(senseId));
    }

    public async Task<MiniLcm.ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        MiniLcm.ExampleSentence exampleSentence)
    {
        await dataModel.AddChange(await GetClientId(), new CreateExampleSentenceChange(exampleSentence, senseId));
        return await dataModel.GetLatest<ExampleSentence>(exampleSentence.Id) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<MiniLcm.ExampleSentence> update)
    {
        var jsonPatch = update.Patch;
        var patchChange = new JsonPatchChange<ExampleSentence>(exampleSentenceId, jsonPatch, jsonOptions);
        await dataModel.AddChange(await GetClientId(), patchChange);
        return await dataModel.GetLatest<ExampleSentence>(exampleSentenceId) ?? throw new NullReferenceException();
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await dataModel.AddChange(await GetClientId(), new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

    public UpdateBuilder<T> CreateUpdateBuilder<T>() where T : class
    {
        return new UpdateBuilder<T>();
    }
}
