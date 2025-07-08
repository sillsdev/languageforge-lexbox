using System.Data;
using FluentValidation;
using Gridify;
using SIL.Harmony;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.FullTextSearch;
using LcmCrdt.Objects;
using LcmCrdt.Utils;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
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
    LcmCrdtDbContext dbContext,
    IOptions<LcmCrdtConfig> config,
    ILogger<CrdtMiniLcmApi> logger,
    EntrySearchService? entrySearchService = null) : IMiniLcmApi
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
        await AddChanges(changes.Chunk(100));
    }

    private void AssertWritable()
    {
        if (ProjectData.IsReadonly)
            throw new ReadOnlyException($"project is readonly because you are logged in with the {ProjectData.Role} role. If your role recently changed, try refreshing the server project list on the home page.");
    }

    /// <summary>
    /// use when making a large number of changes at once
    /// </summary>
    /// <param name="changeChunks"></param>
    private async Task AddChanges(IEnumerable<IReadOnlyCollection<IChange>> changeChunks)
    {
        AssertWritable();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        foreach (var chunk in changeChunks)
        {
            await dataModel.AddChanges(ClientId, chunk, commitMetadata: NewMetadata(), deferCommit: true);
        }

        await dataModel.FlushDeferredCommits();
        await transaction.CommitAsync();
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

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem)
    {
        var entityId = Guid.NewGuid();
        var exists = await WritingSystems.AnyAsync(ws => ws.WsId == writingSystem.WsId && ws.Type == writingSystem.Type);
        if (exists) throw new DuplicateObjectException($"Writing system {writingSystem.WsId.Code} already exists");
        var wsCount = await WritingSystems.CountAsync(ws => ws.Type == writingSystem.Type);
        await AddChange(new CreateWritingSystemChange(writingSystem, entityId, wsCount));
        return await GetWritingSystem(writingSystem.WsId, writingSystem.Type) ?? throw new NullReferenceException();
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
        await WritingSystemSync.Sync(before, after, api ?? this);
        return await GetWritingSystem(after.WsId, after.Type) ?? throw new NullReferenceException("unable to find writing system with id " + after.WsId);
    }

    private WritingSystem? _defaultVernacularWs;
    private WritingSystem? _defaultAnalysisWs;
    private async ValueTask<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        if (id.Code == "default")
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
        await AddChange(new Changes.SetOrderChange<ComplexFormComponent>(component.Id, order));
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

    public async Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        options ??= FilterQueryOptions.Default;
        var (queryable, _) = await FilterEntries(Entries, query, options);
        return await queryable.CountAsync();
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return SearchEntries(null, options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string? query, QueryOptions? options = null)
    {
        return GetEntries(Entries, query, options);
    }

    private async IAsyncEnumerable<Entry> GetEntries(IQueryable<Entry> queryable,
        string? query = null,
        QueryOptions? options = null)
    {
        options ??= QueryOptions.Default;
        (queryable, var sortingHandled) = await FilterEntries(queryable, query, options);
        if (!sortingHandled)
            queryable = await ApplySorting(queryable, options, query);

        queryable = queryable
            .LoadWith(e => e.Senses)
            .ThenLoad(s => s.ExampleSentences)
            .LoadWith(e => e.Senses).ThenLoad(s => s.PartOfSpeech)
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .AsQueryable();

        queryable = options.ApplyPaging(queryable);
        var complexFormComparer = cultureProvider.GetCompareInfo(await GetWritingSystem(default, WritingSystemType.Vernacular))
            .AsComplexFormComparer();
        var entries = queryable.AsAsyncEnumerable();
        await foreach (var entry in EfExtensions.SafeIterate(entries))
        {
            entry.ApplySortOrder(complexFormComparer);
            yield return entry;
        }
    }

    private async Task<(IQueryable<Entry> queryable, bool sortingHandled)> FilterEntries(IQueryable<Entry> queryable,
        string? query,
        FilterQueryOptions options)
    {
        bool sortingHandled = false;
        if (!string.IsNullOrEmpty(query))
        {
            if (entrySearchService is not null && entrySearchService.ValidSearchTerm(query))
            {
                var queryOptions = options as QueryOptions;
                //ranking must be done at the same time as part of the full-text search, so we can't use normal sorting
                sortingHandled = queryOptions?.Order.Field == SortField.SearchRelevance;
                queryable = entrySearchService.FilterAndRank(queryable,
                    query,
                    sortingHandled,
                    queryOptions?.Order.Ascending == true);
            }
            else
            {
                queryable = queryable.Where(Filtering.SearchFilter(query));
            }
        }

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
        return (queryable, sortingHandled);
    }

    private async ValueTask<IQueryable<Entry>> ApplySorting(IQueryable<Entry> queryable, QueryOptions options, string? query = null)
    {
        var sortWs = await GetWritingSystem(options.Order.WritingSystem, WritingSystemType.Vernacular);
        if (sortWs is null)
            throw new NullReferenceException($"sort writing system {options.Order.WritingSystem} not found");

        switch (options.Order.Field)
        {
            case SortField.SearchRelevance:
                return queryable.ApplyRoughBestMatchOrder(options.Order with { WritingSystem = sortWs.WsId }, query);
            case SortField.Headword:
                var ordered = options.ApplyOrder(queryable, e => e.Headword(sortWs.WsId).CollateUnicode(sortWs));
                return ordered.ThenBy(e => e.Id);
            default:
                throw new ArgumentOutOfRangeException(nameof(options), "sort field unknown " + options.Order.Field);
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
        //we're using this change list to ensure that we partially commit in case of an error
        //this lets us attempt an import again skipping the entries that were already imported
        var changeList = new List<IChange>(1300);
        var createdEntryIds = await Entries.Select(e => e.Id).ToAsyncEnumerable().ToHashSetAsync();
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
        var updatedEntry = await GetEntry(id) ?? throw new NullReferenceException("unable to find entry with id " + id);
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
        if (sense.PartOfSpeechId.HasValue && await GetPartOfSpeech(sense.PartOfSpeechId.Value) is null)
            throw new InvalidOperationException($"Part of speech must exist when creating a sense (could not find GUID {sense.PartOfSpeechId.Value})");

        sense.Order = await OrderPicker.PickOrder(Senses.Where(s => s.EntryId == entryId), between);
        await AddChanges(await CreateSenseChanges(entryId, sense).ToArrayAsync());
        return await GetSense(entryId, sense.Id) ?? throw new NullReferenceException("unable to find sense " + sense.Id);
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        var sense = await GetSense(entryId, senseId);
        if (sense is null) throw new NullReferenceException($"unable to find sense with id {senseId}");
        await AddChanges([..sense.ToChanges(update.Patch)]);
        return await GetSense(entryId, senseId) ?? throw new NullReferenceException("unable to find sense with id " + senseId);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
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
