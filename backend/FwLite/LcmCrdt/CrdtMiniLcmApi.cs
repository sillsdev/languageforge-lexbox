using System.Linq.Expressions;
using FluentValidation;
using Gridify;
using SIL.Harmony;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.Objects;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using MiniLcm.Validators;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using MiniLcm.Culture;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class CrdtMiniLcmApi(
    DataModel dataModel,
    CurrentProjectService projectService,
    IMiniLcmCultureProvider cultureProvider,
    MiniLcmValidators validators,
    IOptions<LcmCrdtConfig> config) : IMiniLcmApi
{
    private Guid ClientId { get; } = projectService.ProjectData.ClientId;
    public ProjectData ProjectData => projectService.ProjectData;
    private LcmCrdtConfig LcmConfig => config.Value;

    private IQueryable<Entry> Entries => dataModel.QueryLatest<Entry>();
    private IQueryable<ComplexFormComponent> ComplexFormComponents => dataModel.QueryLatest<ComplexFormComponent>();
    private IQueryable<ComplexFormType> ComplexFormTypes => dataModel.QueryLatest<ComplexFormType>();
    private IQueryable<Sense> Senses => dataModel.QueryLatest<Sense>();
    private IQueryable<ExampleSentence> ExampleSentences => dataModel.QueryLatest<ExampleSentence>();
    private IQueryable<WritingSystem> WritingSystems => dataModel.QueryLatest<WritingSystem>();
    private IQueryable<SemanticDomain> SemanticDomains => dataModel.QueryLatest<SemanticDomain>();
    private IQueryable<PartOfSpeech> PartsOfSpeech => dataModel.QueryLatest<PartOfSpeech>();
    private IQueryable<Publication> Publications => dataModel.QueryLatest<Publication>();

    private CommitMetadata NewMetadata()
    {
        var metadata = new CommitMetadata
        {
            ClientVersion = AppVersion.Version,
            //todo, if a user logs out and in with another account, this will be out of date until the next sync
            AuthorName = ProjectData.LastUserName,
            AuthorId = ProjectData.LastUserId
        };
        return metadata;
    }
    private async Task<Commit> AddChange(IChange change)
    {
        var commit = await dataModel.AddChange(ClientId, change, commitMetadata: NewMetadata());
        return commit;
    }

    private async Task<Commit> AddChanges(IEnumerable<IChange> changes)
    {
        var commit = await dataModel.AddChanges(ClientId, changes, commitMetadata: NewMetadata());
        return commit;
    }

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
            await AddChange(new CreateWritingSystemChange(writingSystem, type, entityId, wsCount));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException e) when (e.InnerException is SqliteException { SqliteErrorCode: 19 }) //19 is a unique constraint violation
        {
            throw new DuplicateObjectException($"Writing system {writingSystem.WsId.Code} already exists", e);
        }
        return await GetWritingSystem(writingSystem.WsId, type) ?? throw new NullReferenceException();
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        var ws = await GetWritingSystem(id, type);
        if (ws is null) throw new NullReferenceException($"unable to find writing system with id {id}");
        var patchChange = new JsonPatchChange<WritingSystem>(ws.Id, update.Patch);
        await AddChange(patchChange);
        return await GetWritingSystem(id, type) ?? throw new NullReferenceException();
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await WritingSystemSync.Sync(before, after, api ?? this);
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

    public IAsyncEnumerable<Publication> GetPublications()
    {
        return Publications.AsAsyncEnumerable();
    }

    public async Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return await PartsOfSpeech.SingleOrDefaultAsync(pos => pos.Id == id);
    }

    public async Task<Publication?> GetPublication(Guid id)
    {
        return await Publications.SingleOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        if (partOfSpeech.Id == Guid.Empty) partOfSpeech.Id = Guid.NewGuid();
        await validators.ValidateAndThrow(partOfSpeech);
        await AddChange(new CreatePartOfSpeechChange(partOfSpeech.Id, partOfSpeech.Name, partOfSpeech.Predefined));
        return await GetPartOfSpeech(partOfSpeech.Id) ?? throw new NullReferenceException();
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        var pos = await GetPartOfSpeech(id);
        if (pos is null) throw new NullReferenceException($"unable to find part of speech with id {id}");

        await AddChanges(pos.ToChanges(update.Patch));
        return await GetPartOfSpeech(id) ?? throw new NullReferenceException();
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        await validators.ValidateAndThrow(after);
        await PartOfSpeechSync.Sync(before, after, api ?? this);
        return await GetPartOfSpeech(after.Id) ?? throw new NullReferenceException($"unable to find part of speech with id {after.Id}");
    }

    public async Task DeletePartOfSpeech(Guid id)
    {
        await AddChange(new DeleteChange<PartOfSpeech>(id));
    }

    public async Task<Publication> CreatePublication(Publication pub)
    {
        await AddChange(new CreatePublicationChange(pub.Id, pub.Name));
        return await GetPublication(pub.Id) ?? throw new NullReferenceException();

    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        var pub = await GetPublication(id);
        if(pub is null) throw new NullReferenceException($"unable to find publication with id {id}");
        await AddChanges(pub.ToChanges(update.Patch));
        return await GetPublication(id) ?? throw new NullReferenceException("Update resulted in missing publication (invalid patching to a new id?)");
    }

    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        throw new NotImplementedException();
    }

    public async Task DeletePublication(Guid id)
    {
        await AddChange(new DeleteChange<Publication>(id));
    }

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        throw new NotImplementedException();
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        throw new NotImplementedException();
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
        await AddChange(new CreateSemanticDomainChange(semanticDomain.Id, semanticDomain.Name, semanticDomain.Code, semanticDomain.Predefined));
        return await GetSemanticDomain(semanticDomain.Id) ?? throw new NullReferenceException();
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        var semDom = await GetSemanticDomain(id);
        if (semDom is null) throw new NullReferenceException($"unable to find semantic domain with id {id}");

        await AddChanges(semDom.ToChanges(update.Patch));
        return await GetSemanticDomain(id) ?? throw new NullReferenceException();
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await SemanticDomainSync.Sync(before, after, api ?? this);
        return await GetSemanticDomain(after.Id) ?? throw new NullReferenceException($"unable to find semantic domain with id {after.Id}");
    }

    public async Task DeleteSemanticDomain(Guid id)
    {
        await AddChange( new DeleteChange<SemanticDomain>(id));
    }

    public async Task BulkImportSemanticDomains(IEnumerable<MiniLcm.Models.SemanticDomain> semanticDomains)
    {
        await AddChanges(semanticDomains.Select(sd => new CreateSemanticDomainChange(sd.Id, sd.Name, sd.Code, sd.Predefined)));
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
        await AddChange(new CreateComplexFormType(complexFormType.Id, complexFormType.Name));
        return await ComplexFormTypes.SingleAsync(c => c.Id == complexFormType.Id);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        await AddChange(new JsonPatchChange<ComplexFormType>(id, update.Patch));
        return await GetComplexFormType(id) ?? throw new NullReferenceException($"unable to find complex form type with id {id}");
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await ComplexFormTypeSync.Sync(before, after, api ?? this);
        return await GetComplexFormType(after.Id) ?? throw new NullReferenceException($"unable to find complex form type with id {after.Id}");
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        await AddChange(new DeleteChange<ComplexFormType>(id));
    }

    private async Task<AddEntryComponentChange> CreateComplexFormComponentChange(ComplexFormComponent complexFormComponent, BetweenPosition? between = null)
    {
        complexFormComponent.Order = await OrderPicker.PickOrder(ComplexFormComponents.Where(c => c.ComplexFormEntryId == complexFormComponent.ComplexFormEntryId), between);
        return new AddEntryComponentChange(complexFormComponent);
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        // tests in seperate PR:
        // 1: call create with the same ID should throw.
        // 2: change a propertyId => should result in a new ID (in Crdt test not base).
        // 3: "normal" create also changes provided ID.
        var existing = await ComplexFormComponents.SingleOrDefaultAsync(c =>
            c.ComplexFormEntryId == complexFormComponent.ComplexFormEntryId
            && c.ComponentEntryId == complexFormComponent.ComponentEntryId
            && c.ComponentSenseId == complexFormComponent.ComponentSenseId);
        if (existing is null)
        {
            var betweenIds = between is null ? null : new BetweenPosition(between.Previous?.Id, between.Next?.Id);
            var addEntryComponentChange = await CreateComplexFormComponentChange(complexFormComponent, betweenIds);
            await AddChange(addEntryComponentChange);
            return (await ComplexFormComponents.SingleOrDefaultAsync(c => c.Id == addEntryComponentChange.EntityId)) ?? throw NotFoundException.ForType<ComplexFormComponent>();
        }
        else if (between is not null)
        {
            await MoveComplexFormComponent(existing, between);
        }
        return existing;
    }

    public async Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        var betweenIds = new BetweenPosition(between.Previous?.Id, between.Next?.Id);
        var order = await OrderPicker.PickOrder(ComplexFormComponents.Where(s => s.ComplexFormEntryId == component.ComplexFormEntryId), betweenIds);
        await dataModel.AddChange(ClientId, new Changes.SetOrderChange<ComplexFormComponent>(component.Id, order));
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await AddChange(new DeleteChange<ComplexFormComponent>(complexFormComponent.Id));
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await AddChange(new AddComplexFormTypeChange(entryId, await ComplexFormTypes.SingleAsync(ct => ct.Id == complexFormTypeId)));
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await AddChange(new RemoveComplexFormTypeChange(entryId, complexFormTypeId));
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
        var queryable = Entries;
        if (predicate is not null) queryable = queryable.Where(predicate);
        if (options.Exemplar is not null)
        {
            var ws = (await GetWritingSystem(options.Exemplar.WritingSystem, WritingSystemType.Vernacular))?.WsId;
            if (ws is null)
                throw new NullReferenceException($"writing system {options.Exemplar.WritingSystem} not found");
            queryable = queryable.WhereExemplar(ws.Value, options.Exemplar.Value);
        }

        if (options.Filter?.GridifyFilter != null)
        {
            queryable = queryable.ApplyFiltering(options.Filter.GridifyFilter, LcmConfig.Mapper);
        }

        var sortWs = (await GetWritingSystem(options.Order.WritingSystem, WritingSystemType.Vernacular));
        if (sortWs is null)
            throw new NullReferenceException($"sort writing system {options.Order.WritingSystem} not found");
        queryable = queryable
            .LoadWith(e => e.Senses)
            .ThenLoad(s => s.ExampleSentences)
            .LoadWith(e => e.Senses).ThenLoad(s => s.PartOfSpeech)
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .AsQueryable()
            .OrderBy(e => e.Headword(sortWs.WsId).CollateUnicode(sortWs))
            .ThenBy(e => e.Id);
        queryable = options.ApplyPaging(queryable);
        var complexFormComparer = cultureProvider.GetCompareInfo(sortWs)
            .AsComplexFormComparer();
        var entries = queryable.AsAsyncEnumerable();
        await foreach (var entry in entries)
        {
            entry.ApplySortOrder(complexFormComparer);
            yield return entry;
        }
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        var entry = await Entries
            .LoadWith(e => e.Senses)
            .ThenLoad(s => s.ExampleSentences)
            .LoadWith(e => e.Senses).ThenLoad(s => s.PartOfSpeech)
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .AsQueryable()
            .SingleOrDefaultAsync(e => e.Id == id);
        if (entry is not null)
        {
            var sortWs = await GetWritingSystem(WritingSystemId.Default, WritingSystemType.Vernacular);
            var complexFormComparer = cultureProvider.GetCompareInfo(sortWs)
                .AsComplexFormComparer();
            entry.ApplySortOrder(complexFormComparer);
        }
        return entry;
    }

    public async Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        var semanticDomains = await SemanticDomains.ToDictionaryAsync(sd => sd.Id, sd => sd);
        await AddChanges(
            entries.ToBlockingEnumerable()
                .SelectMany(entry => CreateEntryChanges(entry, semanticDomains))
                //force entries to be created first, this avoids issues where references are created before the entry is created
                .OrderBy(c => c is CreateEntryChange ? 0 : 1)
        );
    }

    private IEnumerable<IChange> CreateEntryChanges(Entry entry, Dictionary<Guid, SemanticDomain> semanticDomains)
    {
        yield return new CreateEntryChange(entry);

        //only add components, if we add both components and complex forms we'll get duplicates when importing data
        var componentOrder = 1;
        foreach (var component in entry.Components)
        {
            component.Order = componentOrder++;
            yield return new AddEntryComponentChange(component);
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
        await AddChanges([
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
            var currOrder = 1;
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
                if (complexFormComponent.ComplexFormEntryId == entry.Id)
                {
                    // the entry is the complex-form and picks what order its components are in
                    complexFormComponent.Order = currOrder++;
                    yield return new AddEntryComponentChange(complexFormComponent);
                }
                else
                {
                    // the entry is a component, so we let its complex-form pick the order
                    yield return await CreateComplexFormComponentChange(complexFormComponent);
                }
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

        await AddChanges(entry.ToChanges(update.Patch));
        return await GetEntry(id) ?? throw new NullReferenceException("unable to find entry with id " + id);
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await EntrySync.Sync(before, after, api ?? this);
        return await GetEntry(after.Id) ?? throw new NullReferenceException("unable to find entry with id " + after.Id);
    }

    public async Task DeleteEntry(Guid id)
    {
        await AddChange(new DeleteChange<Entry>(id));
    }

    private async IAsyncEnumerable<IChange> CreateSenseChanges(Guid entryId, Sense sense)
    {
        sense.SemanticDomains = await SemanticDomains
            .Where(sd => sense.SemanticDomains.Select(s => s.Id).Contains(sd.Id))
            .ToListAsync();

        yield return new CreateSenseChange(sense, entryId);
        var exampleOrder = 1;
        foreach (var exampleSentence in sense.ExampleSentences)
        {
            exampleSentence.Order = exampleOrder++;
            yield return new CreateExampleSentenceChange(exampleSentence, sense.Id);
        }
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid senseId)
    {
        var sense = await Senses.LoadWith(s => s.PartOfSpeech)
            .AsQueryable()
            .SingleOrDefaultAsync(e => e.Id == senseId);
        sense?.ApplySortOrder();
        return sense;
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(sense);
        if (sense.PartOfSpeechId.HasValue && await GetPartOfSpeech(sense.PartOfSpeechId.Value) is null)
            throw new InvalidOperationException($"Part of speech must exist when creating a sense (could not find GUID {sense.PartOfSpeechId.Value})");

        sense.Order = await OrderPicker.PickOrder(Senses.Where(s => s.EntryId == entryId), between);
        await dataModel.AddChanges(ClientId, await CreateSenseChanges(entryId, sense).ToArrayAsync());
        return await GetSense(entryId, sense.Id) ?? throw new NullReferenceException("unable to find sense " + sense.Id);
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        var sense = await GetSense(entryId, senseId);
        if (sense is null) throw new NullReferenceException($"unable to find sense with id {senseId}");
        await dataModel.AddChanges(ClientId, [..sense.ToChanges(update.Patch)]);
        return await GetSense(entryId, senseId) ?? throw new NullReferenceException("unable to find sense with id " + senseId);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await SenseSync.Sync(entryId, before, after, api ?? this);
        return await GetSense(entryId, after.Id) ?? throw new NullReferenceException("unable to find sense with id " + after.Id);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        var order = await OrderPicker.PickOrder(Senses.Where(s => s.EntryId == entryId), between);
        await AddChange(new Changes.SetOrderChange<Sense>(senseId, order));
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await AddChange(new DeleteChange<Sense>(senseId));
    }

    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        await AddChange(new AddSemanticDomainChange(semanticDomain, senseId));
    }

    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await AddChange(new RemoveSemanticDomainChange(semanticDomainId, senseId));
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(exampleSentence);
        exampleSentence.Order = await OrderPicker.PickOrder(ExampleSentences.Where(s => s.SenseId == senseId), between);
        await AddChange(new CreateExampleSentenceChange(exampleSentence, senseId));
        return await GetExampleSentence(entryId, senseId, exampleSentence.Id) ?? throw new NullReferenceException();
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        var exampleSentence = await ExampleSentences
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
        await AddChange(patchChange);
        return await GetExampleSentence(entryId, senseId, exampleSentenceId) ?? throw new NullReferenceException();
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await ExampleSentenceSync.Sync(entryId, senseId, before, after, api ?? this);
        return await GetExampleSentence(entryId, senseId, after.Id) ?? throw new NullReferenceException();
    }

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        var order = await OrderPicker.PickOrder(ExampleSentences.Where(s => s.SenseId == senseId), between);
        await AddChange(new Changes.SetOrderChange<ExampleSentence>(exampleId, order));
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await AddChange(new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

    public void Dispose()
    {
    }
}
