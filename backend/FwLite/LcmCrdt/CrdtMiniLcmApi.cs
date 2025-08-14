using System.Data;
using Gridify;
using SIL.Harmony;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.FullTextSearch;
using LcmCrdt.MediaServer;
using LcmCrdt.Objects;
using LcmCrdt.Utils;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Core;
using MiniLcm.Culture;
using MiniLcm.Media;
using SystemTextJsonPatch;

namespace LcmCrdt;

public class CrdtMiniLcmApi(
    DataModel dataModel,
    CurrentProjectService projectService,
    MiniLcmRepositoryFactory repoFactory,
    IOptions<LcmCrdtConfig> config,
    ILogger<CrdtMiniLcmApi> logger,
    LcmMediaService lcmMediaService,
    EntrySearchService? entrySearchService = null) : IMiniLcmApi
{
    private Guid ClientId { get; } = projectService.ProjectData.ClientId;
    public ProjectData ProjectData => projectService.ProjectData;
    private LcmCrdtConfig LcmConfig => config.Value;

    private CommitMetadata NewMetadata()
    {
        var metadata = new CommitMetadata
        {
            ClientVersion = AppVersion.Version,
            //todo, if a user logs out and in with another account, this will be out of date until the next sync
            AuthorName = ProjectData.LastUserName ?? config.Value.DefaultAuthorForCommits,
            AuthorId = ProjectData.LastUserId
        };
        return metadata;
    }
    private async Task<Commit> AddChange(IChange change)
    {
        AssertWritable();
        var commit = await dataModel.AddChange(ClientId, change, commitMetadata: NewMetadata());
        return commit;
    }

    private async Task AddChanges(IEnumerable<IChange> changes)
    {
        AssertWritable();
        await dataModel.AddManyChanges(ClientId, changes, commitMetadata: NewMetadata);
    }

    private void AssertWritable()
    {
        if (ProjectData.IsReadonly)
            throw new ReadOnlyException($"project is readonly because you are logged in with the {ProjectData.Role} role. If your role recently changed, try refreshing the server project list on the home page.");
    }

    public async Task<WritingSystems> GetWritingSystems()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var systems = await repo.WritingSystems
            .OrderBy(ws => ws.Order)
            .ThenBy(ws => ws.WsId)
            .ToArrayAsync();
        return new WritingSystems
        {
            Analysis = [.. systems.Where(ws => ws.Type == WritingSystemType.Analysis)],
            Vernacular = [.. systems.Where(ws => ws.Type == WritingSystemType.Vernacular)]
        };
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var entityId = Guid.NewGuid();
        var wsType = writingSystem.Type;
        var exists = await repo.WritingSystems.AnyAsync(ws => ws.WsId == writingSystem.WsId && ws.Type == wsType);
        if (exists) throw new DuplicateObjectException($"Writing system {writingSystem.WsId.Code} already exists");
        var betweenIds = between is null ? null : await between.MapAsync(async wsId => wsId is null ? null : (await repo.GetWritingSystem(wsId.Value, wsType))?.Id);
        var order = await OrderPicker.PickOrder(repo.WritingSystems.Where(ws => ws.Type == wsType), betweenIds);
        await AddChange(new CreateWritingSystemChange(writingSystem, entityId, order));
        return await repo.GetWritingSystem(writingSystem.WsId, wsType) ?? throw new NullReferenceException();
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var ws = await repo.GetWritingSystem(id, type);
        if (ws is null) throw new NullReferenceException($"unable to find writing system with id {id}");
        var patchChange = new JsonPatchChange<WritingSystem>(ws.Id, update.Patch);
        await AddChange(patchChange);
        return await repo.GetWritingSystem(id, type) ?? throw new NullReferenceException();
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        await WritingSystemSync.Sync(before, after, api ?? this);
        return await GetWritingSystem(after.WsId, after.Type) ?? throw new NullReferenceException("unable to find writing system with id " + after.WsId);
    }

    public async Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var ws = await repo.GetWritingSystem(id, type);
        if (ws is null) throw new NullReferenceException($"unable to find writing system with id {id}");
        var betweenIds = await between.MapAsync(async wsId => wsId is null ? null : (await repo.GetWritingSystem(wsId.Value, type))?.Id);
        var order = await OrderPicker.PickOrder(repo.WritingSystems.Where(s => s.Type == type), betweenIds);
        await AddChange(new Changes.SetOrderChange<WritingSystem>(ws.Id, order));
    }

    public async Task<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetWritingSystem(id, type);
    }

    public async IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var partOfSpeech in repo.PartsOfSpeech.AsAsyncEnumerable())
        {
            yield return partOfSpeech;
        }
    }

    public async Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.PartsOfSpeech.SingleOrDefaultAsync(pos => pos.Id == id);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        if (partOfSpeech.Id == Guid.Empty) partOfSpeech.Id = Guid.NewGuid();
        await AddChange(new CreatePartOfSpeechChange(partOfSpeech.Id, partOfSpeech.Name, partOfSpeech.Predefined));
        return await GetPartOfSpeech(partOfSpeech.Id) ?? throw new NullReferenceException();
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        var pos = await GetPartOfSpeech(id);
        if (pos is null) throw new NullReferenceException($"unable to find part of speech with id {id}");

        await AddChanges(update.Patch.ToChanges(pos.Id));
        return await GetPartOfSpeech(id) ?? throw new NullReferenceException();
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        await PartOfSpeechSync.Sync(before, after, api ?? this);
        return await GetPartOfSpeech(after.Id) ?? throw new NullReferenceException($"unable to find part of speech with id {after.Id}");
    }

    public async Task DeletePartOfSpeech(Guid id)
    {
        await AddChange(new DeleteChange<PartOfSpeech>(id));
    }

    public async IAsyncEnumerable<Publication> GetPublications()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var publication in repo.Publications.AsAsyncEnumerable())
        {
            yield return publication;
        }
    }

    public async Task<Publication?> GetPublication(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetPublication(id);
    }

    public async Task<Publication> CreatePublication(Publication pub)
    {
        await AddChange(new CreatePublicationChange(pub.Id, pub.Name));
        return await GetPublication(pub.Id) ?? throw new NullReferenceException();

    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var pub = await repo.GetPublication(id) ?? throw new NullReferenceException($"Unable to find publication with id {id}");
        await AddChanges(update.Patch.ToChanges(pub.Id));
        return await repo.GetPublication(id) ?? throw new NullReferenceException("Update resulted in missing publication (invalid patching to a new id?)");
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        await PublicationSync.Sync(before, after, api ?? this);
        var updatedPublication = await GetPublication(after.Id) ?? throw new NullReferenceException("Unable to find publication with id " + after.Id);
        return updatedPublication;
    }

    public async Task DeletePublication(Guid id)
    {
        await AddChange(new DeleteChange<Publication>(id));
    }

    public async Task AddPublication(Guid entryId, Guid publicationId)
    {
        var pub = await GetPublication(publicationId) ?? throw new NullReferenceException("Unable to find publication with id {id}");
        await AddChange(new AddPublicationChange(entryId, pub));
    }

    public async Task RemovePublication(Guid entryId, Guid publicationId)
    {
        await AddChange(new RemovePublicationChange(entryId, publicationId));
    }

    public async IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var semanticDomain in repo.SemanticDomains.AsAsyncEnumerable())
        {
            yield return semanticDomain;
        }
    }

    public async Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.SemanticDomains.FirstOrDefaultAsync(semdom => semdom.Id == id);
    }

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        await AddChange(new CreateSemanticDomainChange(semanticDomain.Id, semanticDomain.Name, semanticDomain.Code, semanticDomain.Predefined));
        return await GetSemanticDomain(semanticDomain.Id) ?? throw new NullReferenceException();
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        var semDom = await GetSemanticDomain(id);
        if (semDom is null) throw new NullReferenceException($"unable to find semantic domain with id {id}");

        await AddChanges(update.Patch.ToChanges(semDom.Id));
        return await GetSemanticDomain(id) ?? throw new NullReferenceException();
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        await SemanticDomainSync.Sync(before, after, api ?? this);
        return await GetSemanticDomain(after.Id) ?? throw new NullReferenceException($"unable to find semantic domain with id {after.Id}");
    }

    public async Task DeleteSemanticDomain(Guid id)
    {
        await AddChange(new DeleteChange<SemanticDomain>(id));
    }

    public async Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        await AddChanges(await semanticDomains.Select(sd => new CreateSemanticDomainChange(sd.Id, sd.Name, sd.Code, sd.Predefined)).ToArrayAsync());
    }

    public async IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var complexFormType in repo.ComplexFormTypes.AsAsyncEnumerable())
        {
            yield return complexFormType;
        }
    }

    public async Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.ComplexFormTypes.SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (complexFormType.Id == default) complexFormType.Id = Guid.NewGuid();
        await AddChange(new CreateComplexFormType(complexFormType.Id, complexFormType.Name));
        return await repo.ComplexFormTypes.SingleAsync(c => c.Id == complexFormType.Id);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        await AddChange(new JsonPatchChange<ComplexFormType>(id, update.Patch));
        return await GetComplexFormType(id) ?? throw new NullReferenceException($"unable to find complex form type with id {id}");
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        await ComplexFormTypeSync.Sync(before, after, api ?? this);
        return await GetComplexFormType(after.Id) ?? throw new NullReferenceException($"unable to find complex form type with id {after.Id}");
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        await AddChange(new DeleteChange<ComplexFormType>(id));
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        // tests in seperate PR:
        // 1: call create with the same ID should throw.
        // 2: change a propertyId => should result in a new ID (in Crdt test not base).
        // 3: "normal" create also changes provided ID.
        var existing = await repo.FindComplexFormComponent(complexFormComponent);
        if (existing is null)
        {
            var betweenIds = between is null ? null : new BetweenPosition(between.Previous?.Id, between.Next?.Id);
            var addEntryComponentChange = await repo.CreateComplexFormComponentChange(complexFormComponent, betweenIds);
            await AddChange(addEntryComponentChange);
            return await repo.FindComplexFormComponent(addEntryComponentChange.EntityId);
        }
        else if (between is not null)
        {
            await MoveComplexFormComponent(existing, between);
        }
        return existing;
    }

    public async Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var betweenIds = new BetweenPosition(between.Previous?.Id, between.Next?.Id);
        var order = await OrderPicker.PickOrder(repo.ComplexFormComponents.Where(s => s.ComplexFormEntryId == component.ComplexFormEntryId), betweenIds);
        await AddChange(new Changes.SetOrderChange<ComplexFormComponent>(component.Id, order));
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await AddChange(new DeleteChange<ComplexFormComponent>(complexFormComponent.Id));
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await AddChange(new AddComplexFormTypeChange(entryId, await repo.ComplexFormTypes.SingleAsync(ct => ct.Id == complexFormTypeId)));
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await AddChange(new RemoveComplexFormTypeChange(entryId, complexFormTypeId));
    }

    public IAsyncEnumerable<MorphTypeData> GetAllMorphTypeData()
    {
        throw new NotImplementedException();
    }

    public Task<MorphTypeData?> GetMorphTypeData(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<MorphTypeData> CreateMorphTypeData(MorphTypeData morphTypeData)
    {
        throw new NotImplementedException();
    }

    public Task<MorphTypeData> UpdateMorphTypeData(Guid id, UpdateObjectInput<MorphTypeData> update)
    {
        throw new NotImplementedException();
    }

    public Task<MorphTypeData> UpdateMorphTypeData(MorphTypeData before, MorphTypeData after, IMiniLcmApi? api = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteMorphTypeData(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.CountEntries(query, options);
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return SearchEntries(null, options);
    }

    public async IAsyncEnumerable<Entry> SearchEntries(string? query, QueryOptions? options = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var entry in repo.GetEntries(query, options))
        {
            yield return entry;
        }
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetEntry(id);
    }

    public async Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var semanticDomains = await repo.SemanticDomains.ToDictionaryAsync(sd => sd.Id, sd => sd);
        //we're using this change list to ensure that we partially commit in case of an error
        //this lets us attempt an import again skipping the entries that were already imported
        var changeList = new List<IChange>(1300);
        var createdEntryIds = await repo.Entries.Select(e => e.Id).ToAsyncEnumerable().ToHashSetAsync();
        int entryCount = 0;
        await foreach (var entry in entries)
        {
            entryCount++;
            changeList.AddRange(CreateEntryChanges(entry, semanticDomains, createdEntryIds));
            createdEntryIds.Add(entry.Id);
            if (changeList.Count > 1000)
            {
                await AddChanges(changeList);
                changeList.Clear();
                logger.LogInformation("Added {Count} entries so far", entryCount);
            }
        }
        if (changeList.Count > 0)
        {
            await AddChanges(changeList);
        }

        await (entrySearchService?.RegenerateEntrySearchTable() ?? Task.CompletedTask);
    }

    private IEnumerable<IChange> CreateEntryChanges(Entry entry,
        Dictionary<Guid, SemanticDomain> semanticDomains,
        HashSet<Guid> createdEntryIds)
    {
        yield return new CreateEntryChange(entry);

        var componentOrder = 1;
        foreach (var component in entry.Components)
        {
            //only add components if the component entry was created already, otherwise it will be added when the component entry is created
            if (!createdEntryIds.Contains(component.ComponentEntryId)) continue;
            if (component.Order == 0) component.Order = componentOrder++;
            yield return new AddEntryComponentChange(component);
        }
        foreach (var complexForm in entry.ComplexForms)
        {
            //only add complex forms if the complex form entry was created already, otherwise it will be added when the complex form entry is created
            if (!createdEntryIds.Contains(complexForm.ComplexFormEntryId)) continue;
            yield return new AddEntryComponentChange(complexForm);
        }
        foreach (var addComplexFormTypeChange in entry.ComplexFormTypes.Select(c => new AddComplexFormTypeChange(entry.Id, c)))
        {
            yield return addComplexFormTypeChange;
        }
        foreach (var addPublicationChange in entry.PublishIn.Select(c => new AddPublicationChange(entry.Id, c)))
        {
            yield return addPublicationChange;
        }
        var senseOrder = 1;
        foreach (var sense in entry.Senses)
        {
            sense.SemanticDomains = sense.SemanticDomains
                .Select(sd => semanticDomains.TryGetValue(sd.Id, out var selectedSd) ? selectedSd : null)
                .OfType<SemanticDomain>()
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
        await using var repo = await repoFactory.CreateRepoAsync();
        await AddChanges([
            new CreateEntryChange(entry),
            ..await entry.Senses.ToAsyncEnumerable()
                .SelectMany((s, i) =>
                {
                    s.Order = i + 1;
                    return CreateSenseChanges(entry.Id, s, repo.SemanticDomains);
                })
                .ToArrayAsync(),
            ..await ToPublications(entry.PublishIn).ToArrayAsync(),
            ..await ToComplexFormComponents(entry.Components).ToArrayAsync(),
            ..await ToComplexFormComponents(entry.ComplexForms).ToArrayAsync(),
            ..await ToComplexFormTypes(entry.ComplexFormTypes).ToArrayAsync()
        ]);
        return await repo.GetEntry(entry.Id) ?? throw new NullReferenceException();

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
                    yield return await repo.CreateComplexFormComponentChange(complexFormComponent);
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

                if (!await repo.ComplexFormTypes.AnyAsyncEF(t => t.Id == complexFormType.Id))
                {
                    throw new InvalidOperationException($"Complex form type {complexFormType} does not exist");
                }
                yield return new AddComplexFormTypeChange(entry.Id, complexFormType);
            }
        }

        async IAsyncEnumerable<AddPublicationChange> ToPublications(IList<Publication> publications)
        {
            foreach (var publication in publications)
            {
                if (publication.Id == default)
                {
                    throw new InvalidOperationException("Publication must have an id");
                }

                if (!await repo.Publications.AnyAsyncEF(t => t.Id == publication.Id))
                {
                    throw new InvalidOperationException($"Publication {publication} does not exist");
                }
                yield return new AddPublicationChange(entry.Id, publication);
            }
        }
    }

    private async ValueTask<bool> IsEntryDeleted(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return !await repo.Entries.AnyAsyncEF(e => e.Id == id);
    }

    public async Task<Entry> UpdateEntry(Guid id,
        UpdateObjectInput<Entry> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var entry = await repo.GetEntry(id);
        if (entry is null) throw new NullReferenceException($"unable to find entry with id {id}");

        await AddChanges(update.Patch.ToChanges(entry.Id));
        var updatedEntry = await repo.GetEntry(id) ?? throw new NullReferenceException("unable to find entry with id " + id);
        return updatedEntry;
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        await EntrySync.Sync(before, after, api ?? this);
        var updatedEntry = await GetEntry(after.Id) ?? throw new NullReferenceException("unable to find entry with id " + after.Id);
        return updatedEntry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await AddChange(new DeleteChange<Entry>(id));
    }

    private async IAsyncEnumerable<IChange> CreateSenseChanges(Guid entryId,
        Sense sense,
        IQueryable<SemanticDomain> semanticDomains)
    {
        sense.SemanticDomains = await semanticDomains
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
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetSense(entryId, senseId);
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (sense.PartOfSpeechId.HasValue && await GetPartOfSpeech(sense.PartOfSpeechId.Value) is null)
            throw new InvalidOperationException($"Part of speech must exist when creating a sense (could not find GUID {sense.PartOfSpeechId.Value})");

        sense.Order = await OrderPicker.PickOrder(repo.Senses.Where(s => s.EntryId == entryId), between);
        await AddChanges(await CreateSenseChanges(entryId, sense, repo.SemanticDomains).ToArrayAsync());
        return await repo.GetSense(entryId, sense.Id) ?? throw new NullReferenceException("unable to find sense " + sense.Id);
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(entryId, senseId);
        if (sense is null) throw new NullReferenceException($"unable to find sense with id {senseId}");
        await AddChanges(update.Patch.ToChanges(sense.Id));
        return await repo.GetSense(entryId, senseId) ?? throw new NullReferenceException("unable to find sense with id " + senseId);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        await SenseSync.Sync(entryId, before, after, api ?? this);
        return await GetSense(entryId, after.Id) ?? throw new NullReferenceException("unable to find sense with id " + after.Id);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var order = await OrderPicker.PickOrder(repo.Senses.Where(s => s.EntryId == entryId), between);
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

    public async Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        await AddChange(new SetPartOfSpeechChange(senseId, partOfSpeechId));
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        exampleSentence.Order = await OrderPicker.PickOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between);
        await AddChange(new CreateExampleSentenceChange(exampleSentence, senseId));
        return await repo.GetExampleSentence(entryId, senseId, exampleSentence.Id) ?? throw new NullReferenceException();
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetExampleSentence(entryId, senseId, id);
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
        await ExampleSentenceSync.Sync(entryId, senseId, before, after, api ?? this);
        return await GetExampleSentence(entryId, senseId, after.Id) ?? throw new NullReferenceException();
    }

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var order = await OrderPicker.PickOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between);
        await AddChange(new Changes.SetOrderChange<ExampleSentence>(exampleId, order));
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await AddChange(new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

    public async Task<ReadFileResponse> GetFileStream(MediaUri mediaUri)
    {
        if (mediaUri == MediaUri.NotFound) return new ReadFileResponse(ReadFileResult.NotFound);
        return await lcmMediaService.GetFileStream(mediaUri.FileId);
    }

    public async Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        try
        {
            if (stream.SafeLength() > MediaFile.MaxFileSize) return new UploadFileResponse(UploadFileResult.TooBig);
            var (result, newResource) = await lcmMediaService.SaveFile(stream, metadata);
            var mediaUri = new MediaUri(result.Id, ProjectData.ServerId ?? "lexbox.org");
            return new UploadFileResponse(mediaUri, savedToLexbox: result.Remote, newResource);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to save file {Filename}", metadata.Filename);
            return new UploadFileResponse(e.Message);
        }
    }

    public void Dispose()
    {
    }
}
