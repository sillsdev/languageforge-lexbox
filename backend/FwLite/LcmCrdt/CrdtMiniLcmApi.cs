using System.Linq.Expressions;
using FluentValidation;
using SIL.Harmony;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.Objects;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using MiniLcm.Validators;
using SIL.Harmony.Db;

namespace LcmCrdt;

public class CrdtMiniLcmApi(DataModel dataModel, CurrentProjectService projectService, LcmCrdtDbContext dbContext, MiniLcmValidators validators) : IMiniLcmApi
{
    private Guid ClientId { get; } = projectService.ProjectData.ClientId;
    public ProjectData ProjectData => projectService.ProjectData;

    private IQueryable<Entry> Entries => dataModel.QueryLatest<Entry>().AsTracking(false);
    private IQueryable<ComplexFormComponent> ComplexFormComponents => dataModel.QueryLatest<ComplexFormComponent>()
        .AsTracking(false);
    private IQueryable<ComplexFormType> ComplexFormTypes => dataModel.QueryLatest<ComplexFormType>().AsTracking(false);
    private IQueryable<Sense> Senses => dataModel.QueryLatest<Sense>().AsTracking(false);
    private IQueryable<ExampleSentence> ExampleSentences => dataModel.QueryLatest<ExampleSentence>().AsTracking(false);
    private IQueryable<WritingSystem> WritingSystems => dataModel.QueryLatest<WritingSystem>().AsTracking(false);
    private IQueryable<SemanticDomain> SemanticDomains => dataModel.QueryLatest<SemanticDomain>().AsTracking(false);
    private IQueryable<PartOfSpeech> PartsOfSpeech => dataModel.QueryLatest<PartOfSpeech>().AsTracking(false);

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
        await validators.ValidateAndThrow(writingSystem);
        var entityId = Guid.NewGuid();
        var wsCount = await WritingSystems.CountAsync(ws => ws.Type == type);
        try
        {
            await dataModel.AddChange(ClientId, new CreateWritingSystemChange(writingSystem, type, entityId, wsCount));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException e) when (e.InnerException is SqliteException { SqliteErrorCode: 19 }) //19 is a unique constraint violation
        {
            throw new DuplicateObjectException($"Writing system {writingSystem.WsId.Code} already exists", e);
        }
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

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after)
    {
        await validators.ValidateAndThrow(after);
        await WritingSystemSync.Sync(before, after, this);
        return await GetWritingSystem(after.WsId, after.Type) ?? throw new NullReferenceException("unable to find writing system with id " + after.WsId);
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

    public Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return dataModel.GetLatest<PartOfSpeech>(id);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        await validators.ValidateAndThrow(partOfSpeech);
        await dataModel.AddChange(ClientId, new CreatePartOfSpeechChange(partOfSpeech.Id, partOfSpeech.Name, partOfSpeech.Predefined));
        return await GetPartOfSpeech(partOfSpeech.Id) ?? throw new NullReferenceException();
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        var pos = await GetPartOfSpeech(id);
        if (pos is null) throw new NullReferenceException($"unable to find part of speech with id {id}");

        await dataModel.AddChanges(ClientId, [..pos.ToChanges(update.Patch)]);
        return await GetPartOfSpeech(id) ?? throw new NullReferenceException();
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after)
    {
        await validators.ValidateAndThrow(after);
        await PartOfSpeechSync.Sync(before, after, this);
        return await GetPartOfSpeech(after.Id) ?? throw new NullReferenceException($"unable to find part of speech with id {after.Id}");
    }

    public async Task DeletePartOfSpeech(Guid id)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<PartOfSpeech>(id));
    }

    public IAsyncEnumerable<MiniLcm.Models.SemanticDomain> GetSemanticDomains()
    {
        return SemanticDomains.AsAsyncEnumerable();
    }

    public Task<MiniLcm.Models.SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return SemanticDomains.FirstOrDefaultAsync(semdom => semdom.Id == id);
    }

    public async Task<MiniLcm.Models.SemanticDomain> CreateSemanticDomain(MiniLcm.Models.SemanticDomain semanticDomain)
    {
        await validators.ValidateAndThrow(semanticDomain);
        await dataModel.AddChange(ClientId, new CreateSemanticDomainChange(semanticDomain.Id, semanticDomain.Name, semanticDomain.Code, semanticDomain.Predefined));
        return await GetSemanticDomain(semanticDomain.Id) ?? throw new NullReferenceException();
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        var semDom = await GetSemanticDomain(id);
        if (semDom is null) throw new NullReferenceException($"unable to find semantic domain with id {id}");

        await dataModel.AddChanges(ClientId, [..semDom.ToChanges(update.Patch)]);
        return await GetSemanticDomain(id) ?? throw new NullReferenceException();
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after)
    {
        await validators.ValidateAndThrow(after);
        await SemanticDomainSync.Sync(before, after, this);
        return await GetSemanticDomain(after.Id) ?? throw new NullReferenceException($"unable to find semantic domain with id {after.Id}");
    }

    public async Task DeleteSemanticDomain(Guid id)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<SemanticDomain>(id));
    }

    public async Task BulkImportSemanticDomains(IEnumerable<MiniLcm.Models.SemanticDomain> semanticDomains)
    {
        await dataModel.AddChanges(ClientId, semanticDomains.Select(sd => new CreateSemanticDomainChange(sd.Id, sd.Name, sd.Code, sd.Predefined)));
    }

    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return ComplexFormTypes.AsAsyncEnumerable();
    }

    public async Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return await ComplexFormTypes.SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        await validators.ValidateAndThrow(complexFormType);
        if (complexFormType.Id == default) complexFormType.Id = Guid.NewGuid();
        await dataModel.AddChange(ClientId, new CreateComplexFormType(complexFormType.Id, complexFormType.Name));
        return await ComplexFormTypes.SingleAsync(c => c.Id == complexFormType.Id);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        await dataModel.AddChange(ClientId, new JsonPatchChange<ComplexFormType>(id, update.Patch));
        return await GetComplexFormType(id) ?? throw new NullReferenceException($"unable to find complex form type with id {id}");
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after)
    {
        await validators.ValidateAndThrow(after);
        await ComplexFormTypeSync.Sync(before, after, this);
        return await GetComplexFormType(after.Id) ?? throw new NullReferenceException($"unable to find complex form type with id {after.Id}");
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<ComplexFormType>(id));
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var existing = await ComplexFormComponents.SingleOrDefaultAsync(c =>
            c.ComplexFormEntryId == complexFormComponent.ComplexFormEntryId
            && c.ComponentEntryId == complexFormComponent.ComponentEntryId
            && c.ComponentSenseId == complexFormComponent.ComponentSenseId);
        if (existing is not null) return existing;
        var addEntryComponentChange = new AddEntryComponentChange(complexFormComponent);
        await dataModel.AddChange(ClientId, addEntryComponentChange);
        return (await ComplexFormComponents.SingleOrDefaultAsync(c => c.Id == addEntryComponentChange.EntityId)) ?? throw NotFoundException.ForType<ComplexFormComponent>();
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<ComplexFormComponent>(complexFormComponent.Id));
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

        var sortWs = (await GetWritingSystem(options.Order.WritingSystem, WritingSystemType.Vernacular));
        if (sortWs is null)
            throw new NullReferenceException($"sort writing system {options.Order.WritingSystem} not found");
        queryable = queryable
            .LoadWith(e => e.Senses)
            .ThenLoad(s => s.ExampleSentences)
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .AsQueryable()
            .OrderBy(e => e.Headword(sortWs.WsId).CollateUnicode(sortWs))
            .ThenBy(e => e.Id);
        queryable = options.ApplyPaging(queryable);
        var entries = queryable.AsAsyncEnumerable();
        await foreach (var entry in entries)
        {
            entry.ApplySortOrder();
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
        entry?.ApplySortOrder();
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
        await dataModel.AddChanges(ClientId,
            await entries.SelectAwait(async entry =>
            {
                await validators.ValidateAndThrow(entry);
                return entry;
            })
            .SelectMany(entry =>
            {
                return CreateEntryChanges(entry, semanticDomains, partsOfSpeech).ToAsyncEnumerable();
            })
            //force entries to be created first, this avoids issues where references are created before the entry is created
            .OrderBy(c => c is CreateEntryChange ? 0 : 1)
            .ToArrayAsync()
        );
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
        var senseOrder = 1;
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
            sense.Order = senseOrder++;
            yield return new CreateSenseChange(sense, entry.Id);
            var exampleOrder = 1;
            foreach (var exampleSentence in sense.ExampleSentences)
            {
                exampleSentence.Order = exampleOrder++;
                yield return new CreateExampleSentenceChange(exampleSentence, sense.Id);
            }
        }
    }

    public async Task<Entry> CreateEntry(Entry entry)
    {
        await validators.ValidateAndThrow(entry);
        await dataModel.AddChanges(ClientId,
        [
            new CreateEntryChange(entry),
            ..await entry.Senses.ToAsyncEnumerable()
                .SelectMany((s, i) =>
                {
                    s.Order = i + 1;
                    return CreateSenseChanges(entry.Id, s);
                })
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
                //these tests break under sync when the entry was deleted in a CRDT but that's not yet been synced to FW
                //todo enable these tests when the api is not syncing but being called normally
                // if (complexFormComponent.ComponentEntryId != entry.Id &&
                //     await IsEntryDeleted(complexFormComponent.ComponentEntryId))
                // {
                //     throw new InvalidOperationException($"Complex form component {complexFormComponent} references deleted entry {complexFormComponent.ComponentEntryId} as its component");
                // }
                // if (complexFormComponent.ComplexFormEntryId != entry.Id &&
                //     await IsEntryDeleted(complexFormComponent.ComplexFormEntryId))
                // {
                //     throw new InvalidOperationException($"Complex form component {complexFormComponent} references deleted entry {complexFormComponent.ComplexFormEntryId} as its complex form");
                // }

                // if (complexFormComponent.ComponentSenseId != null &&
                //     !await Senses.AnyAsyncEF(s => s.Id == complexFormComponent.ComponentSenseId.Value))
                // {
                //     throw new InvalidOperationException($"Complex form component {complexFormComponent} references deleted sense {complexFormComponent.ComponentSenseId} as its component");
                // }
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
        return await GetEntry(id) ?? throw new NullReferenceException("unable to find entry with id " + id);
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        await validators.ValidateAndThrow(after);
        await EntrySync.Sync(before, after, this);
        return await GetEntry(after.Id) ?? throw new NullReferenceException("unable to find entry with id " + after.Id);
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
        var exampleOrder = 1;
        foreach (var exampleSentence in sense.ExampleSentences)
        {
            exampleSentence.Order = exampleOrder++;
            yield return new CreateExampleSentenceChange(exampleSentence, sense.Id);
        }
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid id)
    {
        var entry = await Entries.AsTracking(false)
            .LoadWith(e => e.Senses)
            .ThenLoad(s => s.ExampleSentences)
            .AsQueryable()
            .SingleOrDefaultAsync(e => e.Id == entryId);
        var sense = entry?.Senses.FirstOrDefault(s => s.Id == id);
        sense?.ApplySortOrder();
        return sense;
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(sense);
        sense.Order = await OrderPicker.PickOrder(Senses.Where(s => s.EntryId == entryId), between);
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

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after)
    {
        await validators.ValidateAndThrow(after);
        await SenseSync.Sync(entryId, before, after, this);
        return await GetSense(entryId, after.Id) ?? throw new NullReferenceException("unable to find sense with id " + after.Id);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        var order = await OrderPicker.PickOrder(Senses.Where(s => s.EntryId == entryId), between);
        await dataModel.AddChange(ClientId, new Changes.SetOrderChange<Sense>(senseId, order));
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<Sense>(senseId));
    }

    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        await dataModel.AddChange(ClientId, new AddSemanticDomainChange(semanticDomain, senseId));
    }

    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await dataModel.AddChange(ClientId, new RemoveSemanticDomainChange(semanticDomainId, senseId));
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(exampleSentence);
        exampleSentence.Order = await OrderPicker.PickOrder(ExampleSentences.Where(s => s.SenseId == senseId), between);
        await dataModel.AddChange(ClientId, new CreateExampleSentenceChange(exampleSentence, senseId));
        return await dataModel.GetLatest<ExampleSentence>(exampleSentence.Id) ?? throw new NullReferenceException();
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        var exampleSentence = await ExampleSentences.AsTracking(false)
            .AsQueryable()
            .SingleOrDefaultAsync(e => e.Id == id);
        return exampleSentence;
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

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after)
    {
        await validators.ValidateAndThrow(after);
        await ExampleSentenceSync.Sync(entryId, senseId, before, after, this);
        return await GetExampleSentence(entryId, senseId, after.Id) ?? throw new NullReferenceException();
    }

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        var order = await OrderPicker.PickOrder(ExampleSentences.Where(s => s.SenseId == senseId), between);
        await dataModel.AddChange(ClientId, new Changes.SetOrderChange<ExampleSentence>(exampleId, order));
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await dataModel.AddChange(ClientId, new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

    public void Dispose()
    {
    }
}
