﻿using System.Linq.Expressions;
using SIL.Harmony;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.Objects;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Db;

namespace LcmCrdt;

public class CrdtMiniLcmApi(DataModel dataModel, CurrentProjectService projectService, LcmCrdtDbContext dbContext) : IMiniLcmApi
{
    private Guid ClientId { get; } = projectService.ProjectData.ClientId;
    public ProjectData ProjectData => projectService.ProjectData;

    private IQueryable<Entry> Entries => dataModel.QueryLatest<Entry>();
    private IQueryable<ComplexFormComponent> ComplexFormComponents => dataModel.QueryLatest<ComplexFormComponent>();
    private IQueryable<ComplexFormType> ComplexFormTypes => dataModel.QueryLatest<ComplexFormType>();
    private IQueryable<Sense> Senses => dataModel.QueryLatest<Sense>();
    private IQueryable<ExampleSentence> ExampleSentences => dataModel.QueryLatest<ExampleSentence>();
    private IQueryable<WritingSystem> WritingSystems => dataModel.QueryLatest<WritingSystem>();
    private IQueryable<SemanticDomain> SemanticDomains => dataModel.QueryLatest<SemanticDomain>();
    private IQueryable<PartOfSpeech> PartsOfSpeech => dataModel.QueryLatest<PartOfSpeech>();

    public async Task<WritingSystems> GetWritingSystems()
    {
        var systems = await WritingSystems.ToArrayAsync();
        return new WritingSystems
        {
            Analysis = systems.Where(ws => ws.Type == WritingSystemType.Analysis)
                .Select(w => ((WritingSystem)w)).ToArray(),
            Vernacular = systems.Where(ws => ws.Type == WritingSystemType.Vernacular)
                .Select(w => ((WritingSystem)w)).ToArray()
        };
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        var entityId = Guid.NewGuid();
        var wsCount = await WritingSystems.CountAsync(ws => ws.Type == type);
        await dataModel.AddChange(ClientId, new CreateWritingSystemChange(writingSystem, type, entityId, wsCount));
        return await dataModel.GetLatest<WritingSystem>(entityId) ?? throw new NullReferenceException();
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        var ws = await GetWritingSystem(id, type);
        if (ws is null) throw new NullReferenceException($"unable to find writing system with id {id}");
        var patchChange = new JsonPatchChange<WritingSystem>(ws.Id, update.Patch);
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
            } ?? throw new NullReferenceException($"Unable to find a default writing system of type {type}");
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
        if (complexFormType.Id == default) complexFormType.Id = Guid.NewGuid();
        await dataModel.AddChange(ClientId, new CreateComplexFormType(complexFormType.Id, complexFormType.Name));
        return await ComplexFormTypes.SingleAsync(c => c.Id == complexFormType.Id);
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var addEntryComponentChange = new AddEntryComponentChange(complexFormComponent);
        await dataModel.AddChange(ClientId, addEntryComponentChange);
        return await ComplexFormComponents.SingleAsync(c => c.Id == addEntryComponentChange.EntityId);
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<ComplexFormComponent>(complexFormComponent.Id));
    }

    public async Task ReplaceComplexFormComponent(ComplexFormComponent old, ComplexFormComponent @new)
    {
        IChange change;
        if (old.ComplexFormEntryId != @new.ComplexFormEntryId)
        {
            change = SetComplexFormComponentChange.NewComplexForm(old.Id, @new.ComplexFormEntryId);
        }
        else if (old.ComponentEntryId != @new.ComponentEntryId)
        {
            change = SetComplexFormComponentChange.NewComponent(old.Id, @new.ComponentEntryId);
        }
        else if (old.ComponentSenseId != @new.ComponentSenseId)
        {
            change = SetComplexFormComponentChange.NewComponentSense(old.Id,
                @new.ComponentEntryId,
                @new.ComponentSenseId);
        }
        else
        {
            return;
        }
        await dataModel.AddChange(ClientId, change);
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await dataModel.AddChange(ClientId, new AddComplexFormTypeChange(entryId, await ComplexFormTypes.SingleAsync(ct => ct.Id == complexFormTypeId)));
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await dataModel.AddChange(ClientId, new RemoveComplexFormTypeChange(entryId, complexFormTypeId));
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return GetEntriesAsyncEnum(predicate: null, options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string? query, QueryOptions? options = null)
    {
        if (string.IsNullOrEmpty(query)) return GetEntriesAsyncEnum(null, options);

        return GetEntriesAsyncEnum(Filtering.SearchFilter(query), options);
    }

    private IAsyncEnumerable<Entry> GetEntriesAsyncEnum(
        Expression<Func<Entry, bool>>? predicate = null,
        QueryOptions? options = null)
    {
        return GetEntries(predicate, options);
    }

    private async IAsyncEnumerable<Entry> GetEntries(
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
            .LoadWith(e => e.Senses).ThenLoad(s => s.ExampleSentences)
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .AsQueryable()
            .OrderBy(e => e.Headword(sortWs.Value))
            .ThenBy(e => e.Id)
            .Skip(options.Offset)
            .Take(options.Count);
        var entries = queryable.AsAsyncEnumerable();
        await foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        var entry = await Entries.AsTracking(false)
            .LoadWith(e => e.Senses)
            .ThenLoad(s => s.ExampleSentences)
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .AsQueryable()
            .SingleOrDefaultAsync(e => e.Id == id);
        return entry;
    }

    /// <summary>
    /// does not return the newly created entry, used for importing a large amount of data
    /// </summary>
    /// <param name="entry"></param>
    public async Task CreateEntryLite(Entry entry)
    {
        await dataModel.AddChanges(ClientId,
        [
            new CreateEntryChange(entry),
            ..entry.Senses.Select(s => new CreateSenseChange(s, entry.Id)),
            ..entry.Senses.SelectMany(s => s.ExampleSentences,
                (sense, sentence) => new CreateExampleSentenceChange(sentence, sense.Id))
        ], deferCommit: true);
    }

    public async Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        var semanticDomains = await SemanticDomains.ToDictionaryAsync(sd => sd.Id, sd => sd);
        var partsOfSpeech = await PartsOfSpeech.ToDictionaryAsync(p => p.Id, p => p);
        await dataModel.AddChanges(ClientId, entries.ToBlockingEnumerable().SelectMany(entry => CreateEntryChanges(entry, semanticDomains, partsOfSpeech)));
    }

    private IEnumerable<IChange> CreateEntryChanges(Entry entry, Dictionary<Guid, SemanticDomain> semanticDomains, Dictionary<Guid, PartOfSpeech> partsOfSpeech)
    {
        yield return new CreateEntryChange(entry);

        //only add components, if we add both components and complex forms we'll get duplicates when importing data
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

    public async Task<Entry> CreateEntry(Entry entry)
    {
        await dataModel.AddChanges(ClientId,
        [
            new CreateEntryChange(entry),
            ..await entry.Senses.ToAsyncEnumerable()
                .SelectMany(s => CreateSenseChanges(entry.Id, s))
                .ToArrayAsync(),
            ..await ToComplexFormComponents(entry.Components).ToArrayAsync(),
            ..await ToComplexFormComponents(entry.ComplexForms).ToArrayAsync(),
            ..await ToComplexFormTypes(entry.ComplexFormTypes).ToArrayAsync()
        ]);
        return await GetEntry(entry.Id) ?? throw new NullReferenceException();

        async IAsyncEnumerable<AddEntryComponentChange> ToComplexFormComponents(IList<ComplexFormComponent> complexFormComponents)
        {
            foreach (var complexFormComponent in complexFormComponents)
            {
                if (complexFormComponent.ComponentEntryId == default) complexFormComponent.ComponentEntryId = entry.Id;
                if (complexFormComponent.ComplexFormEntryId == default) complexFormComponent.ComplexFormEntryId = entry.Id;
                if (complexFormComponent.ComponentEntryId == complexFormComponent.ComplexFormEntryId)
                {
                    throw new InvalidOperationException($"Complex form component {complexFormComponent} has the same component id as its complex form");
                }
                if (complexFormComponent.ComponentEntryId != entry.Id &&
                    await IsEntryDeleted(complexFormComponent.ComponentEntryId))
                {
                    throw new InvalidOperationException($"Complex form component {complexFormComponent} references deleted entry {complexFormComponent.ComponentEntryId} as its component");
                }
                if (complexFormComponent.ComplexFormEntryId != entry.Id &&
                    await IsEntryDeleted(complexFormComponent.ComplexFormEntryId))
                {
                    throw new InvalidOperationException($"Complex form component {complexFormComponent} references deleted entry {complexFormComponent.ComplexFormEntryId} as its complex form");
                }

                if (complexFormComponent.ComponentSenseId != null &&
                    !await Senses.AnyAsyncEF(s => s.Id == complexFormComponent.ComponentSenseId.Value))
                {
                    throw new InvalidOperationException($"Complex form component {complexFormComponent} references deleted sense {complexFormComponent.ComponentSenseId} as its component");
                }
                yield return new AddEntryComponentChange(complexFormComponent);
            }
        }

        async IAsyncEnumerable<AddComplexFormTypeChange> ToComplexFormTypes(IList<ComplexFormType> complexFormTypes)
        {
            foreach (var complexFormType in complexFormTypes)
            {
                if (complexFormType.Id == default)
                {
                    throw new InvalidOperationException("Complex form type must have an id");
                }

                if (!await ComplexFormTypes.AnyAsyncEF(t => t.Id == complexFormType.Id))
                {
                    throw new InvalidOperationException($"Complex form type {complexFormType} does not exist");
                }
                yield return new AddComplexFormTypeChange(entry.Id, complexFormType);
            }
        }
    }

    private async ValueTask<bool> IsEntryDeleted(Guid id)
    {
        return !await Entries.AnyAsyncEF(e => e.Id == id);
    }



    public async Task<Entry> UpdateEntry(Guid id,
        UpdateObjectInput<Entry> update)
    {
        var entry = await GetEntry(id);
        if (entry is null) throw new NullReferenceException($"unable to find entry with id {id}");

        await dataModel.AddChanges(ClientId, [..entry.ToChanges(update.Patch)]);
        return await GetEntry(id) ?? throw new NullReferenceException();
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        //todo version stuff isn't working yet.
        // if (!Guid.TryParse(entry.Version, out var snapshotId))
        // {
        //     throw new InvalidOperationException($"Unable to parse snapshot id '{entry.Version}'");
        // }
        //
        // //todo this will not work for properties not in the snapshot, but we don't have a way to get the full snapshot yet
        // var beforeChanges = await dataModel.GetBySnapshotId<Entry>(snapshotId);
        // //workaround to avoid syncing senses, which are not in the snapshot
        // beforeChanges.Senses = [];
        // entry.Senses = [];
        await EntrySync.Sync(after, before, this);
        return await GetEntry(after.Id) ?? throw new NullReferenceException();
    }

    public async Task DeleteEntry(Guid id)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<Entry>(id));
    }

    private async IAsyncEnumerable<IChange> CreateSenseChanges(Guid entryId, Sense sense)
    {
        sense.SemanticDomains = await SemanticDomains
            .Where(sd => sense.SemanticDomains.Select(s => s.Id).Contains(sd.Id))
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

    public async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        await dataModel.AddChanges(ClientId, await CreateSenseChanges(entryId, sense).ToArrayAsync());
        return await dataModel.GetLatest<Sense>(sense.Id) ?? throw new NullReferenceException();
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        var sense = await dataModel.GetLatest<Sense>(senseId);
        if (sense is null) throw new NullReferenceException($"unable to find sense with id {senseId}");
        await dataModel.AddChanges(ClientId, [..sense.ToChanges(update.Patch)]);
        return await dataModel.GetLatest<Sense>(senseId) ?? throw new NullReferenceException();
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<Sense>(senseId));
    }

    public async Task AddSemanticDomainToSense(Guid senseId, MiniLcm.Models.SemanticDomain semanticDomain)
    {
        await dataModel.AddChange(ClientId, new AddSemanticDomainChange(semanticDomain, senseId));
    }

    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await dataModel.AddChange(ClientId, new RemoveSemanticDomainChange(semanticDomainId, senseId));
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        await dataModel.AddChange(ClientId, new CreateExampleSentenceChange(exampleSentence, senseId));
        return await dataModel.GetLatest<ExampleSentence>(exampleSentence.Id) ?? throw new NullReferenceException();
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        var jsonPatch = update.Patch;
        var patchChange = new JsonPatchChange<ExampleSentence>(exampleSentenceId, jsonPatch);
        await dataModel.AddChange(ClientId, patchChange);
        return await dataModel.GetLatest<ExampleSentence>(exampleSentenceId) ?? throw new NullReferenceException();
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

}
