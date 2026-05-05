using System.Data;
using SIL.Harmony;
using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.CustomJsonPatches;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Changes.ExampleSentences;
using LcmCrdt.Data;
using LcmCrdt.FullTextSearch;
using LcmCrdt.MediaServer;
using LcmCrdt.Objects;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Core;
using MiniLcm.Culture;
using MiniLcm.Media;

namespace LcmCrdt;

public class CrdtMiniLcmApi(
    DataModel dataModel,
    CurrentProjectService projectService,
    MiniLcmRepositoryFactory repoFactory,
    IOptions<LcmCrdtConfig> config,
    ILogger<CrdtMiniLcmApi> logger,
    LcmMediaService lcmMediaService,
    EntrySearchService? entrySearchService = null,
    UpdateEntrySearchTableInterceptor? ftsInterceptor = null) : IMiniLcmApi
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
    private List<IChange>? _batchedChanges;
    private Dictionary<string, OrderPicker.BatchOrderScope>? _batchOrderScopes;
    private bool IsBatching => _batchedChanges is not null;

    /// <summary>
    /// Begins a bulk change batch. Every subsequent <c>AddChange</c> is queued into an
    /// in-memory list instead of committing individually; the list is flushed via one call
    /// to <c>DataModel.AddManyChanges</c> when the returned scope disposes. For sync-scale
    /// workloads this collapses thousands of transactions into one and suppresses per-change
    /// FTS work until a single post-flush regeneration, which is the difference between
    /// a 30-minute sync and a 21-second sync.
    /// </summary>
    /// <remarks>
    /// <para><b>This method is internal by design.</b> The deferred-write semantics have
    /// sharp edges (below) that callers must honour. The only production caller today is
    /// <c>CrdtFwdataProjectSyncService.SyncInternal</c>, whose write patterns are pinned
    /// by <c>BulkChangeBatchSharpEdgesTests</c>. A new consumer should extend that test file
    /// before shipping.</para>
    /// <para><b>Safe-use contract:</b></para>
    /// <list type="bullet">
    ///   <item>DB reads inside the scope (GetEntry, GetSense, etc.) see the pre-scope state
    ///     — queued creates/updates are not visible until the scope disposes.</item>
    ///   <item>Update*/Move*/Add* methods that pre-fetch the target entity will throw
    ///     <see cref="NotFoundException"/> if the target was created earlier in the same batch.
    ///     Do not mix Create+Update of the same entity inside one scope.</item>
    ///   <item>Metadata entities (WritingSystem, PartOfSpeech, Publication, SemanticDomain,
    ///     ComplexFormType) that subsequent Creates reference must be committed *outside*
    ///     the scope — the referential existence checks
    ///     (<c>repo.ComplexFormTypes.AnyAsync</c>, <c>GetPartOfSpeech</c>, etc.) throw at
    ///     queue time otherwise.</item>
    ///   <item>Order-picking among same-batch siblings is made collision-free for the common
    ///     append case via a per-scope high-water-mark bump. Cross-region inserts (e.g.,
    ///     interleaving inserts at the start and end of the same parent) can still produce
    ///     non-ideal orderings that the sync's second-pass reconciliation may correct.</item>
    ///   <item>Duplicate <c>CreateEntry</c> for the same ID inside one scope: the second is
    ///     silently dropped by Harmony's SnapshotWorker (the change doesn't
    ///     <c>SupportsApplyChange</c>).</item>
    ///   <item>If the batch flush throws, the transaction rolls back and the FTS interceptor
    ///     is re-enabled without regenerating — the DB and FTS end up in the pre-scope state.</item>
    /// </list>
    /// </remarks>
    internal BulkChangeBatchScope BeginBulkChangeBatch()
    {
        if (_batchedChanges is not null)
            throw new InvalidOperationException("Cannot nest bulk change batches");
        _batchedChanges = [];
        _batchOrderScopes = [];
        // Suppress FTS interceptor — will regenerate after flush
        if (ftsInterceptor is not null) ftsInterceptor.SuppressUpdates = true;
        return new BulkChangeBatchScope(this, dataModel, ftsInterceptor, entrySearchService);
    }

    internal sealed class BulkChangeBatchScope : IAsyncDisposable
    {
        private readonly CrdtMiniLcmApi _api;
        private readonly DataModel _dataModel;
        private readonly UpdateEntrySearchTableInterceptor? _ftsInterceptor;
        private readonly EntrySearchService? _entrySearchService;
        public int QueuedChangeCount => _api._batchedChanges?.Count ?? 0;

        internal BulkChangeBatchScope(CrdtMiniLcmApi api, DataModel dataModel,
            UpdateEntrySearchTableInterceptor? ftsInterceptor, EntrySearchService? entrySearchService)
        {
            _api = api;
            _dataModel = dataModel;
            _ftsInterceptor = ftsInterceptor;
            _entrySearchService = entrySearchService;
        }

        public async ValueTask DisposeAsync()
        {
            var changes = _api._batchedChanges;
            _api._batchedChanges = null;
            _api._batchOrderScopes = null;
            // Always re-enable the FTS interceptor, even if flush throws, so later writes
            // in the same scope (or error-recovery paths) update FTS incrementally.
            if (_ftsInterceptor is not null) _ftsInterceptor.SuppressUpdates = false;
            if (changes is null or []) return;

            _api.AssertWritable();
            await _dataModel.AddManyChanges(_api.ClientId, changes, commitMetadata: _api.NewMetadata);
            // Only regenerate the FTS table after a successful flush. AddManyChanges runs
            // inside a transaction, so a failure leaves the DB (and therefore the FTS table)
            // untouched. Regenerating on failure would also risk masking the original exception.
            if (_entrySearchService is not null)
                await _entrySearchService.RegenerateEntrySearchTable();
        }
    }

    /// <summary>
    /// Picks a sibling order that stays correct inside a bulk batch. We thread a per-scope
    /// <see cref="OrderPicker.BatchOrderScope"/> through <see cref="OrderPicker.PickOrder{T}"/>
    /// so the picker can (a) resolve a caller's <c>between</c> anchor against a queued
    /// create/move of that anchor instead of its stale DB row, and (b) produce append-case
    /// orders that never land below something we already issued or below an existing DB
    /// sibling. After the pick, we record the new entity's order in the scope so subsequent
    /// picks that reference it as <c>between.Previous</c> or <c>between.Next</c> see its real
    /// in-batch order. Outside a batch this is a pass-through.
    /// </summary>
    private async Task<double> PickBatchAwareOrder<T>(
        IQueryable<T> siblings,
        BetweenPosition? between,
        string scopeKey,
        Guid entityId)
        where T : class, IOrderableNoId, IObjectWithId
    {
        OrderPicker.BatchOrderScope? scope = null;
        if (_batchOrderScopes is not null)
        {
            if (!_batchOrderScopes.TryGetValue(scopeKey, out scope))
            {
                scope = new OrderPicker.BatchOrderScope();
                _batchOrderScopes[scopeKey] = scope;
            }
        }

        var order = await OrderPicker.PickOrder(siblings, between, scope);

        if (scope is not null)
        {
            scope.EntityOrders[entityId] = order;
            if (order > scope.MaxIssuedOrder) scope.MaxIssuedOrder = order;
        }
        return order;
    }

    private async Task<Commit> AddChange(IChange change)
    {
        if (_batchedChanges is not null)
        {
            _batchedChanges.Add(change);
            return default!;
        }
        AssertWritable();
        var commit = await dataModel.AddChange(ClientId, change, commitMetadata: NewMetadata());
        return commit;
    }

    private async Task AddChanges(IEnumerable<IChange> changes)
    {
        if (_batchedChanges is not null)
        {
            _batchedChanges.AddRange(changes);
            return;
        }
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
        var systems = await repo.WritingSystemsOrdered.ToArrayAsync();
        return new WritingSystems
        {
            Analysis = [.. systems.Where(ws => ws.Type == WritingSystemType.Analysis)],
            Vernacular = [.. systems.Where(ws => ws.Type == WritingSystemType.Vernacular)]
        };
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var entityId = writingSystem.MaybeId ?? Guid.NewGuid();
        var wsType = writingSystem.Type;
        var exists = await repo.WritingSystems.AnyAsync(ws => ws.WsId == writingSystem.WsId && ws.Type == wsType);
        if (exists) throw new DuplicateObjectException($"Writing system {writingSystem.WsId.Code} ({wsType}) already exists");
        var betweenIds = between is null ? null : await between.MapAsync(async wsId => wsId is null ? null : (await repo.GetWritingSystem(wsId.Value, wsType))?.Id);
        var order = await PickBatchAwareOrder(repo.WritingSystems.Where(ws => ws.Type == wsType), betweenIds, $"WS:{wsType}", entityId);
        await AddChange(new CreateWritingSystemChange(writingSystem, entityId, order));
        if (IsBatching) return writingSystem;
        return await repo.GetWritingSystem(writingSystem.WsId, wsType) ?? throw NotFoundException.ForWs(writingSystem);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var ws = await repo.GetWritingSystem(id, type) ?? throw NotFoundException.ForWs(id, type);
        var patchChange = new JsonPatchChange<WritingSystem>(ws.Id, update.Patch);
        await AddChange(patchChange);
        if (IsBatching) return ws;
        return await repo.GetWritingSystem(id, type) ?? throw NotFoundException.ForWs(id, type);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        await WritingSystemSync.Sync(before, after, api ?? this);
        return await GetWritingSystem(after.WsId, after.Type) ?? throw NotFoundException.ForWs(after);
    }

    public async Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var ws = await repo.GetWritingSystem(id, type) ?? throw NotFoundException.ForWs(id, type);
        var betweenIds = await between.MapAsync(async wsId => wsId is null ? null : (await repo.GetWritingSystem(wsId.Value, type))?.Id);
        var order = await PickBatchAwareOrder(repo.WritingSystems.Where(s => s.Type == type), betweenIds, $"WS:{type}", ws.Id);
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
        if (IsBatching) return partOfSpeech;
        return await GetPartOfSpeech(partOfSpeech.Id) ?? throw NotFoundException.ForType<PartOfSpeech>(partOfSpeech.Id);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        var pos = await GetPartOfSpeech(id) ?? throw NotFoundException.ForType<PartOfSpeech>(id);

        await AddChanges(update.Patch.ToChanges(pos.Id));
        if (IsBatching) return pos;
        return await GetPartOfSpeech(id) ?? throw NotFoundException.ForType<PartOfSpeech>(id);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        await PartOfSpeechSync.Sync(before, after, api ?? this);
        return await GetPartOfSpeech(after.Id) ?? throw NotFoundException.ForType<PartOfSpeech>(after.Id);
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
        if (IsBatching) return pub;
        return await GetPublication(pub.Id) ?? throw NotFoundException.ForType<Publication>(pub.Id);

    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var pub = await repo.GetPublication(id) ?? throw NotFoundException.ForType<Publication>(id);
        await AddChanges(update.Patch.ToChanges(pub.Id));
        if (IsBatching) return pub;
        return await repo.GetPublication(id) ?? throw NotFoundException.ForType<Publication>($"{id} (invalid patching to a new id?)");
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        await PublicationSync.Sync(before, after, api ?? this);
        var updatedPublication = await GetPublication(after.Id) ?? throw NotFoundException.ForType<Publication>(after.Id);
        return updatedPublication;
    }

    public async Task DeletePublication(Guid id)
    {
        await AddChange(new DeleteChange<Publication>(id));
    }

    public async Task AddPublication(Guid entryId, Guid publicationId)
    {
        var pub = await GetPublication(publicationId) ?? throw NotFoundException.ForType<Publication>(publicationId);
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
        if (IsBatching) return semanticDomain;
        return await GetSemanticDomain(semanticDomain.Id) ?? throw NotFoundException.ForType<SemanticDomain>(semanticDomain.Id);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        var semDom = await GetSemanticDomain(id) ?? throw NotFoundException.ForType<SemanticDomain>(id);
        await AddChanges(update.Patch.ToChanges(semDom.Id));
        if (IsBatching) return semDom;
        return await GetSemanticDomain(id) ?? throw NotFoundException.ForType<SemanticDomain>(id);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        await SemanticDomainSync.Sync(before, after, api ?? this);
        return await GetSemanticDomain(after.Id) ?? throw NotFoundException.ForType<SemanticDomain>(after.Id);
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
        if (IsBatching) return complexFormType;
        return await repo.ComplexFormTypes.SingleAsync(c => c.Id == complexFormType.Id);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        var existing = await GetComplexFormType(id) ?? throw NotFoundException.ForType<ComplexFormType>(id);
        await AddChange(new JsonPatchChange<ComplexFormType>(id, update.Patch));
        if (IsBatching) return existing;
        return await GetComplexFormType(id) ?? throw NotFoundException.ForType<ComplexFormType>(id);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        await ComplexFormTypeSync.Sync(before, after, api ?? this);
        return await GetComplexFormType(after.Id) ?? throw NotFoundException.ForType<ComplexFormType>(after.Id);
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        await AddChange(new DeleteChange<ComplexFormType>(id));
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var existing = await repo.FindComplexFormComponent(complexFormComponent);
        if (existing is null)
        {
            var betweenIds = between is null ? null : await between.MapAsync(async c => (await repo.FindComplexFormComponent(c))?.Id);
            // Always generate a new entity ID — the caller's ID is never used.
            // This aligns with FwData (which ignores the ID entirely) and prevents
            // Harmony duplicate-ID pitfalls during sync (see EntrySync.ComplexFormsDiffApi.Add).
            complexFormComponent.Id = Guid.NewGuid();
            complexFormComponent.Order = await PickBatchAwareOrder(
                repo.ComplexFormComponents.Where(c => c.ComplexFormEntryId == complexFormComponent.ComplexFormEntryId),
                betweenIds,
                $"CFC:{complexFormComponent.ComplexFormEntryId}",
                complexFormComponent.Id);
            var addEntryComponentChange = new AddEntryComponentChange(complexFormComponent);
            await AddChange(addEntryComponentChange);
            if (IsBatching) return complexFormComponent;
            return await repo.FindComplexFormComponent(addEntryComponentChange.EntityId);
        }

        if (between is not null)
        {
            await MoveComplexFormComponent(existing, between);
        }
        return existing;
    }

    public async Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var betweenIds = await between.MapAsync(async c => (await repo.FindComplexFormComponent(c))?.Id);
        // Resolve the component's Id first: PickBatchAwareOrder needs to record the issued
        // order against the moved entity so later picks that reference it as an anchor see
        // the new order, not the stale DB one.
        var id = component.MaybeId ??
                 (await repo.FindComplexFormComponent(component))?.Id
                 ?? throw NotFoundException.ForType<ComplexFormComponent>("missing ID");
        var order = await PickBatchAwareOrder(
            repo.ComplexFormComponents.Where(s => s.ComplexFormEntryId == component.ComplexFormEntryId),
            betweenIds,
            $"CFC:{component.ComplexFormEntryId}",
            id);
        await AddChange(new Changes.SetOrderChange<ComplexFormComponent>(id, order));
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var existing = await repo.FindComplexFormComponent(complexFormComponent);
        if (existing is null) return;
        await AddChange(new DeleteChange<ComplexFormComponent>(existing.Id));
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

    public async Task<int> GetEntryIndex(Guid entryId, string? query = null, IndexQueryOptions? options = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetEntryIndex(entryId, query, options);
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

    public async Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        options ??= CreateEntryOptions.Everything;
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
            ..options.IncludeComplexFormsAndComponents ?
                await ToComplexFormComponents(entry.Components).ToArrayAsync() :
                Enumerable.Empty<AddEntryComponentChange>(),
            ..options.IncludeComplexFormsAndComponents ?
                await ToComplexFormComponents(entry.ComplexForms).ToArrayAsync() :
                Enumerable.Empty<AddEntryComponentChange>(),
            ..await ToComplexFormTypes(entry.ComplexFormTypes).ToArrayAsync()
        ]);
        if (IsBatching)
        {
            // Return a copy consistent with what the DB would return —
            // Components/ComplexForms are managed separately and shouldn't appear in the returned entry
            return new Entry
            {
                Id = entry.Id,
                LexemeForm = entry.LexemeForm,
                CitationForm = entry.CitationForm,
                LiteralMeaning = entry.LiteralMeaning,
                Note = entry.Note,
                MorphType = entry.MorphType,
                Senses = entry.Senses,
                ComplexFormTypes = entry.ComplexFormTypes,
                PublishIn = entry.PublishIn,
                Components = [],
                ComplexForms = [],
            };
        }
        return await repo.GetEntry(entry.Id) ?? throw NotFoundException.ForType<Entry>(entry.Id);

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
                    if (complexFormComponent.Id == default) complexFormComponent.Id = Guid.NewGuid();
                    complexFormComponent.Order = await PickBatchAwareOrder(
                        repo.ComplexFormComponents.Where(c => c.ComplexFormEntryId == complexFormComponent.ComplexFormEntryId),
                        between: null,
                        $"CFC:{complexFormComponent.ComplexFormEntryId}",
                        complexFormComponent.Id);
                    yield return new AddEntryComponentChange(complexFormComponent);
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
        var entry = await repo.GetEntry(id) ?? throw NotFoundException.ForType<Entry>(id);
        await AddChanges(update.Patch.ToChanges(entry.Id));
        if (IsBatching) return entry;
        var updatedEntry = await repo.GetEntry(id) ?? throw NotFoundException.ForType<Entry>(id);
        return updatedEntry;
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        await EntrySync.SyncFull(before, after, api ?? this);
        var updatedEntry = await GetEntry(after.Id) ?? throw NotFoundException.ForType<Entry>(after.Id);
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

        if (sense.Id == default) sense.Id = Guid.NewGuid();
        sense.Order = await PickBatchAwareOrder(repo.Senses.Where(s => s.EntryId == entryId), between, $"Sense:{entryId}", sense.Id);
        await AddChanges(await CreateSenseChanges(entryId, sense, repo.SemanticDomains).ToArrayAsync());
        if (IsBatching) return sense;
        return await repo.GetSense(entryId, sense.Id) ?? throw NotFoundException.ForType<Sense>(sense.Id);
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(entryId, senseId) ?? throw NotFoundException.ForType<Sense>(senseId);
        await AddChanges(update.Patch.ToChanges(sense.Id));
        if (IsBatching) return sense;
        return await repo.GetSense(entryId, senseId) ?? throw NotFoundException.ForType<Sense>(senseId);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        await SenseSync.Sync(entryId, before, after, api ?? this);
        return await GetSense(entryId, after.Id) ?? throw NotFoundException.ForType<Sense>(after.Id);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var order = await PickBatchAwareOrder(repo.Senses.Where(s => s.EntryId == entryId), between, $"Sense:{entryId}", senseId);
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
        if (exampleSentence.Id == default) exampleSentence.Id = Guid.NewGuid();
        exampleSentence.Order = await PickBatchAwareOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between, $"ExampleSentence:{senseId}", exampleSentence.Id);
        await AddChange(new CreateExampleSentenceChange(exampleSentence, senseId));
        if (IsBatching) return exampleSentence;
        return await repo.GetExampleSentence(entryId, senseId, exampleSentence.Id) ?? throw NotFoundException.ForType<ExampleSentence>(exampleSentence.Id);
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
        var existing = await GetExampleSentence(entryId, senseId, exampleSentenceId) ?? throw NotFoundException.ForType<ExampleSentence>(exampleSentenceId);
        var jsonPatch = update.Patch;
        var patchChange = new JsonPatchExampleSentenceChange(exampleSentenceId, jsonPatch);
        await AddChange(patchChange);
        if (IsBatching) return existing;
        return await GetExampleSentence(entryId, senseId, exampleSentenceId) ?? throw NotFoundException.ForType<ExampleSentence>(exampleSentenceId);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api = null)
    {
        await ExampleSentenceSync.Sync(entryId, senseId, before, after, api ?? this);
        return await GetExampleSentence(entryId, senseId, after.Id) ?? throw NotFoundException.ForType<ExampleSentence>(after.Id);
    }

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var order = await PickBatchAwareOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between, $"ExampleSentence:{senseId}", exampleId);
        await AddChange(new Changes.SetOrderChange<ExampleSentence>(exampleId, order));
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await AddChange(new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

    public async Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        if (translation.Id == Guid.Empty) translation.Id = Guid.NewGuid();
        await AddChange(new AddTranslationChange(exampleSentenceId, translation));
    }

    public async Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        await AddChange(new RemoveTranslationChange(exampleSentenceId, translationId));
    }

    public async Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        var jsonPatch = update.Patch;
        await AddChange(new UpdateTranslationChange(exampleSentenceId, translationId, jsonPatch));
    }

    [Obsolete($"Use {nameof(AddTranslation)} instead")]
    public async Task SetFirstTranslationIds(IDictionary<Guid, Guid> exampleSentenceIdToTranslationId)
    {
        var changes = exampleSentenceIdToTranslationId
            .Select(kv => GetSetFirstTranslationIdChange(kv.Key, kv.Value));
        await AddChanges(changes);

        static SetFirstTranslationIdChange GetSetFirstTranslationIdChange(Guid exampleSentenceId, Guid translationId)
        {
            // When calling this, the first translation of the relevant example-sentence should almost definitely
            // be Translation.MissingTranslationId, which the API maps to the example sentence's DefaultFirstTranslationId.
            // However, there are edge cases, which are probably valid. See the comment above the caling code in CrdtRepairs.
            if (translationId == Translation.MissingTranslationId) throw new InvalidOperationException("Cannot set the first translation id to the missing id placeholder");
            // We could also validate that translationId is not the default first translation ID,
            // but it doesn't really matter if it is. It would just be unexpected.
            return new SetFirstTranslationIdChange(exampleSentenceId, translationId);
        }
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

    public async IAsyncEnumerable<CustomView> GetCustomViews()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var customView in repo.CustomViews.AsAsyncEnumerable())
        {
            yield return customView;
        }
    }

    public async Task<CustomView?> GetCustomView(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetCustomView(id);
    }

    public async Task<CustomView> CreateCustomView(CustomView customView)
    {
        AssertManagerRoleForCustomViewWrite();
        if (customView.Id == Guid.Empty) customView.Id = Guid.NewGuid();
        await AddChange(new CreateCustomViewChange(customView.Id, customView));
        return await GetCustomView(customView.Id) ?? throw NotFoundException.ForType<CustomView>(customView.Id);
    }

    public async Task<CustomView> UpdateCustomView(CustomView customView)
    {
        AssertManagerRoleForCustomViewWrite();
        await using var repo = await repoFactory.CreateRepoAsync();
        var id = customView.Id;
        var _ = await repo.GetCustomView(id) ?? throw NotFoundException.ForType<CustomView>(id);
        await AddChange(new EditCustomViewChange(id, customView));
        return await repo.GetCustomView(id) ?? throw NotFoundException.ForType<CustomView>(id);
    }

    public async Task DeleteCustomView(Guid id)
    {
        AssertManagerRoleForCustomViewWrite();
        await using var repo = await repoFactory.CreateRepoAsync();
        _ = await repo.GetCustomView(id) ?? throw NotFoundException.ForType<CustomView>(id);
        await AddChange(new DeleteChange<CustomView>(id));
    }

    private void AssertManagerRoleForCustomViewWrite()
    {
        if (ProjectData.Role == UserProjectRole.Manager) return;
        throw new UnauthorizedAccessException(
            $"Only managers can manage custom views.");
    }

    public void Dispose()
    {
    }
}
