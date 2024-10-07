using System.Linq.Expressions;
using System.Text.Json;
using SIL.Harmony.Core;
using SIL.Harmony;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Objects;
using LcmCrdt.Data;
using MiniLcm;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using MiniLcm.Models;
using PartOfSpeech = MiniLcm.Models.PartOfSpeech;
using SemanticDomain = LcmCrdt.Objects.SemanticDomain;

namespace LcmCrdt;

public class CrdtMiniLcmApi(DataModel dataModel, JsonSerializerOptions jsonOptions, IHybridDateTimeProvider timeProvider, CurrentProjectService projectService) : IMiniLcmApi
{
    private Guid ClientId { get; } = projectService.ProjectData.ClientId;


    private IQueryable<Entry> Entries => dataModel.GetLatestObjects<Entry>();
    private IQueryable<CrdtComplexFormComponent> ComplexFormComponents => dataModel.GetLatestObjects<CrdtComplexFormComponent>();
    private IQueryable<CrdtComplexFormType> ComplexFormTypes => dataModel.GetLatestObjects<CrdtComplexFormType>();
    private IQueryable<Sense> Senses => dataModel.GetLatestObjects<Sense>();
    private IQueryable<ExampleSentence> ExampleSentences => dataModel.GetLatestObjects<ExampleSentence>();
    private IQueryable<WritingSystem> WritingSystems => dataModel.GetLatestObjects<WritingSystem>();
    private IQueryable<SemanticDomain> SemanticDomains => dataModel.GetLatestObjects<SemanticDomain>();
    private IQueryable<Objects.PartOfSpeech> PartsOfSpeech => dataModel.GetLatestObjects<Objects.PartOfSpeech>();

    public async Task<WritingSystems> GetWritingSystems()
    {
        var systems = await WritingSystems.ToArrayAsync();
        return new WritingSystems
        {
            Analysis = systems.Where(ws => ws.Type == WritingSystemType.Analysis)
                .Select(w => ((MiniLcm.Models.WritingSystem)w)).ToArray(),
            Vernacular = systems.Where(ws => ws.Type == WritingSystemType.Vernacular)
                .Select(w => ((MiniLcm.Models.WritingSystem)w)).ToArray()
        };
    }

    public async Task<MiniLcm.Models.WritingSystem> CreateWritingSystem(WritingSystemType type, MiniLcm.Models.WritingSystem writingSystem)
    {
        var entityId = Guid.NewGuid();
        var wsCount = await WritingSystems.CountAsync(ws => ws.Type == type);
        await dataModel.AddChange(ClientId, new CreateWritingSystemChange(writingSystem, type, entityId, wsCount));
        return await dataModel.GetLatest<WritingSystem>(entityId) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.Models.WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<MiniLcm.Models.WritingSystem> update)
    {
        var ws = await GetWritingSystem(id, type);
        if (ws is null) throw new NullReferenceException($"unable to find writing system with id {id}");
        var patchChange = new JsonPatchChange<WritingSystem>(ws.Id, update.Patch, jsonOptions);
        await dataModel.AddChange(ClientId, patchChange);
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

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return PartsOfSpeech.AsAsyncEnumerable();
    }

    public async Task CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        await dataModel.AddChange(ClientId, new CreatePartOfSpeechChange(partOfSpeech.Id, partOfSpeech.Name, false));
    }

    public IAsyncEnumerable<MiniLcm.Models.SemanticDomain> GetSemanticDomains()
    {
        return SemanticDomains.AsAsyncEnumerable();
    }

    public async Task CreateSemanticDomain(MiniLcm.Models.SemanticDomain semanticDomain)
    {
        await dataModel.AddChange(ClientId, new CreateSemanticDomainChange(semanticDomain.Id, semanticDomain.Name, semanticDomain.Code));
    }

    public async Task BulkImportSemanticDomains(IEnumerable<MiniLcm.Models.SemanticDomain> semanticDomains)
    {
        await dataModel.AddChanges(ClientId, semanticDomains.Select(sd => new CreateSemanticDomainChange(sd.Id, sd.Name, sd.Code)));
    }

    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return ComplexFormTypes.AsAsyncEnumerable();
    }

    public async Task<ComplexFormType> CreateComplexFormType(MiniLcm.Models.ComplexFormType complexFormType)
    {
        await dataModel.AddChange(ClientId, new CreateComplexFormType(complexFormType.Id, complexFormType.Name));
        return await ComplexFormTypes.SingleAsync(c => c.Id == complexFormType.Id);
    }

    public IAsyncEnumerable<MiniLcm.Models.Entry> GetEntries(QueryOptions? options = null)
    {
        return GetEntriesAsyncEnum(predicate: null, options);
    }

    public IAsyncEnumerable<MiniLcm.Models.Entry> SearchEntries(string? query, QueryOptions? options = null)
    {
        if (string.IsNullOrEmpty(query)) return GetEntriesAsyncEnum(null, options);

        return GetEntriesAsyncEnum(Filtering.SearchFilter(query), options);
    }

    private async IAsyncEnumerable<MiniLcm.Models.Entry> GetEntriesAsyncEnum(
        Expression<Func<Entry, bool>>? predicate = null,
        QueryOptions? options = null)
    {
        var entries = await GetEntries(predicate, options);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    private async Task<MiniLcm.Models.Entry[]> GetEntries(
        Expression<Func<Entry, bool>>? predicate = null,
        QueryOptions? options = null)
    {
        options ??= QueryOptions.Default;
        //todo filter on exemplar options and limit results, and sort
        var queryable = Entries;
        if (predicate is not null) queryable = queryable.Where(predicate);
        if (options.Exemplar is not null)
        {
            var ws = (await GetWritingSystem(options.Exemplar.WritingSystem, WritingSystemType.Vernacular))?.WsId;
            if (ws is null)
                throw new NullReferenceException($"writing system {options.Exemplar.WritingSystem} not found");
            queryable = queryable.WhereExemplar(ws.Value, options.Exemplar.Value);
        }

        var sortWs = (await GetWritingSystem(options.Order.WritingSystem, WritingSystemType.Vernacular))?.WsId;
        if (sortWs is null)
            throw new NullReferenceException($"sort writing system {options.Order.WritingSystem} not found");
        queryable = queryable
            .OrderBy(e => e.Headword(sortWs.Value))
            // .ThenBy(e => e.Id)
            .Skip(options.Offset)
            .Take(options.Count);
        var entries = await queryable
            .ToArrayAsyncLinqToDB();
        await LoadSenses(entries);
        await LoadComplexFormData(entries);

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

    private async Task LoadComplexFormData(Entry[] entries)
    {
        var allComponents = await ComplexFormComponents
            .Where(c => entries.Select(e => e.Id).Contains(c.ComplexFormEntryId) || entries.Select(e => e.Id).Contains(c.ComponentEntryId))
            .ToArrayAsyncEF();
        var componentLookup = allComponents.ToLookup(c => c.ComplexFormEntryId).ToDictionary(c => c.Key, c => c.ToArray());
        var complexFormLookup = allComponents.ToLookup(c => c.ComponentEntryId).ToDictionary(c => c.Key, c => c.ToArray());
        foreach (var entry in entries)
        {
            entry.Components = componentLookup.TryGetValue(entry.Id, out var components) ? components.ToArray() : [];
            entry.ComplexForms = complexFormLookup.TryGetValue(entry.Id, out var complexForms) ? complexForms.ToArray() : [];
        }
    }

    public async Task<MiniLcm.Models.Entry?> GetEntry(Guid id)
    {
        var entry = await Entries.SingleOrDefaultAsync(e => e.Id == id);
        if (entry is null) return null;
        var senses = await Senses
                .Where(s => s.EntryId == id).ToArrayAsyncLinqToDB();
        var exampleSentences = (await ExampleSentences
                .Where(e => senses.Select(s => s.Id).Contains(e.SenseId)).ToArrayAsyncEF())
            .ToLookup(e => e.SenseId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        //could optimize this by doing a single query, but this is easier to read
        entry.Components = [..await ComplexFormComponents.Where(c => c.ComplexFormEntryId == id).ToListAsyncEF()];
        entry.ComplexForms = [..await ComplexFormComponents.Where(c => c.ComponentEntryId == id).ToListAsyncEF()];
        entry.Senses = senses;
        foreach (var sense in entry.Senses)
        {
            sense.ExampleSentences = exampleSentences.TryGetValue(sense.Id, out var sentences) ? sentences.ToArray() : [];
        }

        return entry;
    }

    /// <summary>
    /// does not return the newly created entry, used for importing a large amount of data
    /// </summary>
    /// <param name="entry"></param>
    public async Task CreateEntryLite(MiniLcm.Models.Entry entry)
    {
        await dataModel.AddChanges(ClientId,
        [
            new CreateEntryChange(entry),
            ..entry.Senses.Select(s => new CreateSenseChange(s, entry.Id)),
            ..entry.Senses.SelectMany(s => s.ExampleSentences,
                (sense, sentence) => new CreateExampleSentenceChange(sentence, sense.Id))
        ], deferCommit: true);
    }

    public async Task BulkCreateEntries(IAsyncEnumerable<MiniLcm.Models.Entry> entries)
    {
        var semanticDomains = await SemanticDomains.ToDictionaryAsync(sd => sd.Id, sd => sd);
        var partsOfSpeech = await PartsOfSpeech.ToDictionaryAsync(p => p.Id, p => p);
        await dataModel.AddChanges(ClientId, entries.ToBlockingEnumerable().SelectMany(entry => CreateEntryChanges(entry, semanticDomains, partsOfSpeech)));
    }

    private IEnumerable<IChange> CreateEntryChanges(MiniLcm.Models.Entry entry, Dictionary<Guid, SemanticDomain> semanticDomains, Dictionary<Guid, Objects.PartOfSpeech> partsOfSpeech)
    {
        yield return new CreateEntryChange(entry);

        //only add components, if we add both components and complex forms we'll get duplicates
        foreach (var addEntryComponentChange in entry.Components.Select(c => new AddEntryComponentChange(c)))
        {
            yield return addEntryComponentChange;
        }
        foreach (var addComplexFormTypeChange in entry.ComplexFormTypes.Select(c => new AddComplexFormTypeChange(entry.Id, c)))
        {
            yield return addComplexFormTypeChange;
        }
        foreach (var sense in entry.Senses)
        {
            sense.SemanticDomains = sense.SemanticDomains
                .Select(sd => semanticDomains.TryGetValue(sd.Id, out var selectedSd) ? selectedSd : null)
                .OfType<MiniLcm.Models.SemanticDomain>()
                .ToList();
            if (sense.PartOfSpeechId is not null && partsOfSpeech.TryGetValue(sense.PartOfSpeechId.Value, out var partOfSpeech))
            {
                sense.PartOfSpeechId = partOfSpeech.Id;
                sense.PartOfSpeech = partOfSpeech.Name["en"] ?? string.Empty;
            }
            yield return new CreateSenseChange(sense, entry.Id);
            foreach (var exampleSentence in sense.ExampleSentences)
            {
                yield return new CreateExampleSentenceChange(exampleSentence, sense.Id);
            }
        }
    }

    public async Task<MiniLcm.Models.Entry> CreateEntry(MiniLcm.Models.Entry entry)
    {
        await dataModel.AddChanges(ClientId,
        [
            new CreateEntryChange(entry),
            ..await entry.Senses.ToAsyncEnumerable()
                .SelectMany(s => CreateSenseChanges(entry.Id, s))
                .ToArrayAsync(),
            ..entry.Components.Select(c => new AddEntryComponentChange(c)),
            ..entry.ComplexForms.Select(c => new AddEntryComponentChange(c)),
            ..entry.ComplexFormTypes.Select(c => new AddComplexFormTypeChange(entry.Id, c))
        ]);
        return await GetEntry(entry.Id) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.Models.Entry> UpdateEntry(Guid id,
        UpdateObjectInput<MiniLcm.Models.Entry> update)
    {
        var entry = await GetEntry(id);
        if (entry is null) throw new NullReferenceException($"unable to find entry with id {id}");

        await dataModel.AddChanges(ClientId, [..Entry.ChangesFromJsonPatch((Entry)entry, update.Patch)]);
        return await GetEntry(id) ?? throw new NullReferenceException();
    }

    public async Task DeleteEntry(Guid id)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<Entry>(id));
    }

    private async IAsyncEnumerable<IChange> CreateSenseChanges(Guid entryId, MiniLcm.Models.Sense sense)
    {
        sense.SemanticDomains = await SemanticDomains
            .Where(sd => sense.SemanticDomains.Select(s => s.Id).Contains(sd.Id))
            .OfType<MiniLcm.Models.SemanticDomain>()
            .ToListAsync();
        if (sense.PartOfSpeechId is not null)
        {
            var partOfSpeech = await PartsOfSpeech.FirstOrDefaultAsync(p => p.Id == sense.PartOfSpeechId);
            sense.PartOfSpeechId = partOfSpeech?.Id;
            sense.PartOfSpeech = partOfSpeech?.Name["en"] ?? string.Empty;
        }

        yield return new CreateSenseChange(sense, entryId);
        foreach (var change in sense.ExampleSentences.Select(sentence =>
                     new CreateExampleSentenceChange(sentence, sense.Id)))
        {
            yield return change;
        }
    }

    public async Task<MiniLcm.Models.Sense> CreateSense(Guid entryId, MiniLcm.Models.Sense sense)
    {
        await dataModel.AddChanges(ClientId, await CreateSenseChanges(entryId, sense).ToArrayAsync());
        return await dataModel.GetLatest<Sense>(sense.Id) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.Models.Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<MiniLcm.Models.Sense> update)
    {
        var sense = await dataModel.GetLatest<Sense>(senseId);
        if (sense is null) throw new NullReferenceException($"unable to find sense with id {senseId}");
        await dataModel.AddChanges(ClientId, [..Sense.ChangesFromJsonPatch(sense, update.Patch)]);
        return await dataModel.GetLatest<Sense>(senseId) ?? throw new NullReferenceException();
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<Sense>(senseId));
    }

    public async Task<MiniLcm.Models.ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        MiniLcm.Models.ExampleSentence exampleSentence)
    {
        await dataModel.AddChange(ClientId, new CreateExampleSentenceChange(exampleSentence, senseId));
        return await dataModel.GetLatest<ExampleSentence>(exampleSentence.Id) ?? throw new NullReferenceException();
    }

    public async Task<MiniLcm.Models.ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<MiniLcm.Models.ExampleSentence> update)
    {
        var jsonPatch = update.Patch;
        var patchChange = new JsonPatchChange<ExampleSentence>(exampleSentenceId, jsonPatch, jsonOptions);
        await dataModel.AddChange(ClientId, patchChange);
        return await dataModel.GetLatest<ExampleSentence>(exampleSentenceId) ?? throw new NullReferenceException();
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

}
