using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.CustomJsonPatches;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Changes.ExampleSentences;
using LcmCrdt.Data;
using LcmCrdt.FullTextSearch;
using LcmCrdt.Harmony;
using LcmCrdt.MiniLcmImp;
using LcmCrdt.Objects;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using MiniLcm.Media;

namespace LcmCrdt;

public class CrdtMiniLcmApi(
    HarmonyChangeWriter harmonyChangeWriter,
    CurrentProjectService projectService,
    MiniLcmRepositoryFactory repoFactory,
    ILogger<CrdtMiniLcmApi> logger,
    CrdtWritingSystemApi writingSystemApi,
    CrdtSemanticDomainsApi semanticDomainsApi,
    CrdtPublicationApi publicationApi,
    CrdtComplexFormComponentApi complexFormComponentApi,
    CrdtMorphTypeApi morphTypeApi,
    CrdtPartsOfSpeechApi partsOfSpeechApi,
    CrdtComplexFormTypesApi complexFormTypesApi,
    CrdtSenseApi senseApi,
    CrdtPictureApi pictureApi,
    CrdtMediaApi mediaApi,
    CrdtCustomViewApi customViewApi,
    CrdtCommentApi commentApi,
    EntrySearchService? entrySearchService = null) : IMiniLcmApi
{
    public ProjectData ProjectData => projectService.ProjectData;
    public CrdtProject Project => projectService.Project;

    // Flush threshold (in accumulated IChange records) for BulkCreateEntries; internal so tests can force multi-batch behavior.
    internal int BulkCreateBatchSize { get; set; } = 1000;

    #region WritingSystemApi
    public Task<WritingSystems> GetWritingSystems()
    {
        return writingSystemApi.GetWritingSystems();
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem,
        BetweenPosition<WritingSystemId?>? between = null)
    {
        return writingSystemApi.CreateWritingSystem(writingSystem, between);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        return writingSystemApi.UpdateWritingSystem(id, type, update);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before,
        WritingSystem after,
        IMiniLcmApi? api = null)
    {
        return writingSystemApi.UpdateWritingSystem(before, after, api ?? this);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        return writingSystemApi.MoveWritingSystem(id, type, between);
    }

    public Task<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        return writingSystemApi.GetWritingSystem(id, type);
    }
    #endregion

    #region PartsOfSpeechApi
    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return partsOfSpeechApi.GetPartsOfSpeech();
    }

    public async Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return await partsOfSpeechApi.GetPartOfSpeech(id);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        return await partsOfSpeechApi.CreatePartOfSpeech(partOfSpeech);
    }

    public async Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        await partsOfSpeechApi.SubmitUpdatePartOfSpeech(id, update);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return await partsOfSpeechApi.UpdatePartOfSpeech(id, update);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        return await partsOfSpeechApi.UpdatePartOfSpeech(before, after, api ?? this);
    }

    public async Task DeletePartOfSpeech(Guid id)
    {
        await partsOfSpeechApi.DeletePartOfSpeech(id);
    }

    public async Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        await partsOfSpeechApi.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }
    #endregion

    #region PublicationApi
    public IAsyncEnumerable<Publication> GetPublications()
    {
        return publicationApi.GetPublications();
    }

    public async Task<Publication?> GetPublication(Guid id)
    {
        return await publicationApi.GetPublication(id);
    }

    public async Task<Publication> CreatePublication(Publication pub)
    {
        return await publicationApi.CreatePublication(pub);
    }

    public async Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await publicationApi.SubmitUpdatePublication(id, update);
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        return await publicationApi.UpdatePublication(id, update);
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        return await publicationApi.UpdatePublication(before, after, api ?? this);
    }

    public async Task DeletePublication(Guid id)
    {
        await publicationApi.DeletePublication(id);
    }

    public async Task AddPublication(Guid entryId, Guid publicationId)
    {
        await publicationApi.AddPublication(entryId, publicationId);
    }

    public async Task RemovePublication(Guid entryId, Guid publicationId)
    {
        await publicationApi.RemovePublication(entryId, publicationId);
    }
    #endregion

    #region SemanticDomainApi
    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return semanticDomainsApi.GetSemanticDomains();
    }

    public async Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return await semanticDomainsApi.GetSemanticDomain(id);
    }

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        return await semanticDomainsApi.CreateSemanticDomain(semanticDomain);
    }

    public async Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        await semanticDomainsApi.SubmitUpdateSemanticDomain(id, update);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return await semanticDomainsApi.UpdateSemanticDomain(id, update);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        return await semanticDomainsApi.UpdateSemanticDomain(before, after, api ?? this);
    }

    public async Task DeleteSemanticDomain(Guid id)
    {
        await semanticDomainsApi.DeleteSemanticDomain(id);
    }

    public async Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        await semanticDomainsApi.BulkImportSemanticDomains(semanticDomains);
    }

    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        await semanticDomainsApi.AddSemanticDomainToSense(senseId, semanticDomain);
    }

    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await semanticDomainsApi.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }
    #endregion

    #region ComplexFormTypeApi
    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return complexFormTypesApi.GetComplexFormTypes();
    }

    public async Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return await complexFormTypesApi.GetComplexFormType(id);
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        return await complexFormTypesApi.CreateComplexFormType(complexFormType);
    }

    public async Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        await complexFormTypesApi.SubmitUpdateComplexFormType(id, update);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return await complexFormTypesApi.UpdateComplexFormType(id, update);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        return await complexFormTypesApi.UpdateComplexFormType(before, after, api ?? this);
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        await complexFormTypesApi.DeleteComplexFormType(id);
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await complexFormTypesApi.AddComplexFormType(entryId, complexFormTypeId);
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await complexFormTypesApi.RemoveComplexFormType(entryId, complexFormTypeId);
    }
    #endregion

    #region ComplexFormComponentApi
    public async Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        await complexFormComponentApi.SubmitCreateComplexFormComponent(complexFormComponent, between);
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        return await complexFormComponentApi.CreateComplexFormComponent(complexFormComponent, between);
    }

    public async Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await complexFormComponentApi.MoveComplexFormComponent(component, between);
    }

    public async Task SubmitMoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await complexFormComponentApi.SubmitMoveComplexFormComponent(component, between);
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await complexFormComponentApi.DeleteComplexFormComponent(complexFormComponent);
    }
    #endregion

    #region MorphTypeApi
    public IAsyncEnumerable<MorphType> GetMorphTypes()
    {
        return morphTypeApi.GetMorphTypes();
    }

    public async Task<MorphType?> GetMorphType(Guid id)
    {
        return await morphTypeApi.GetMorphType(id);
    }

    public async Task<MorphType?> GetMorphType(MorphTypeKind kind)
    {
        return await morphTypeApi.GetMorphType(kind);
    }

    public async Task<MorphType> CreateMorphType(MorphType morphType)
    {
        return await morphTypeApi.CreateMorphType(morphType);
    }

    public async Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        return await morphTypeApi.UpdateMorphType(id, update);
    }

    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null)
    {
        return await morphTypeApi.UpdateMorphType(before, after, api ?? this);
    }
    #endregion

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
            if (changeList.Count > BulkCreateBatchSize)
            {
                await harmonyChangeWriter.AddChanges(changeList);
                changeList.Clear();
                logger.LogInformation("Added {Count} entries so far", entryCount);
            }
        }
        if (changeList.Count > 0)
        {
            await harmonyChangeWriter.AddChanges(changeList);
            changeList.Clear();
        }

        await (entrySearchService?.RegenerateEntrySearchTable() ?? Task.CompletedTask);

        logger.LogInformation("Added {Count} entries", entryCount);
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
        options ??= CreateEntryOptions.WithMainPublication;
        await using var repo = await repoFactory.CreateRepoAsync();

        // This is our primitive logic for now:
        // If 0, we assume the caller did not specify a number, so we're responsible
        // for keeping homograph numbers accurate.
        // That's it.
        // There are other scenarios that we're NOT handling correctly for now:
        // Deletes, headword changes, non 0's for inserted entries coming from fwdata etc.
        // These will be automatically corrected after 2 fw-headless syncs.
        IChange? homographPromotionChange = null;
        if (entry.HomographNumber == 0)
        {
            var resolution = await HomographResolver.ResolveForNewEntry(entry, repo);
            entry.HomographNumber = resolution.NewEntryNumber;
            if (resolution.Promotion is { } promotion)
            {
                var patchDoc = new SystemTextJsonPatch.JsonPatchDocument<Entry>();
                patchDoc.Replace(e => e.HomographNumber, promotion.NewNumber);
                homographPromotionChange = patchDoc.ToChanges(promotion.EntryId).Single();
            }
        }

        if (options.AutoAddMainPublication)
        {
            var mainPublication = await repo.GetMainPublication();
            if (mainPublication is not null && entry.PublishIn.All(pub => pub.Id != mainPublication.Id))
            {
                entry.PublishIn.Add(mainPublication);
            }
        }
        await harmonyChangeWriter.AddChanges((IEnumerable<IChange>)[
            new CreateEntryChange(entry),
            ..homographPromotionChange is null ? [] : new[] { homographPromotionChange },
            ..await entry.Senses.ToAsyncEnumerable()
                .SelectMany((s, i) =>
                {
                    s.Order = i + 1;
                    return CrdtSenseApi.CreateSenseChanges(entry.Id, s, repo.SemanticDomains);
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

    public async Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        await harmonyChangeWriter.AddChanges(update.Patch.ToChanges(id));
    }

    public async Task<Entry> UpdateEntry(Guid id,
        UpdateObjectInput<Entry> update)
    {
        await SubmitUpdateEntry(id, update);
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetEntry(id) ?? throw NotFoundException.ForType<Entry>(id);
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        await EntrySync.SyncFull(before, after, api ?? this);
        var updatedEntry = await GetEntry(after.Id) ?? throw NotFoundException.ForType<Entry>(after.Id);
        return updatedEntry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<Entry>(id));
    }

    #region SenseApi
    public async Task<Sense?> GetSense(Guid senseId)
    {
        return await senseApi.GetSense(senseId);
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid senseId)
    {
        return await senseApi.GetSense(entryId, senseId);
    }

    public async Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await senseApi.SubmitCreateSense(entryId, sense, between);
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        return await senseApi.CreateSense(entryId, sense, between);
    }

    public async Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        await senseApi.SubmitUpdateSense(entryId, senseId, update);
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        return await senseApi.UpdateSense(entryId, senseId, update);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        return await senseApi.UpdateSense(entryId, before, after, api ?? this);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await senseApi.MoveSense(entryId, senseId, between, kind);
    }

    public async Task SubmitMoveSense(Guid entryId, Guid senseId, BetweenPosition position, MoveKind kind = MoveKind.Reorder)
    {
        await senseApi.SubmitMoveSense(entryId, senseId, position, kind);
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await senseApi.DeleteSense(entryId, senseId);
    }
    #endregion

    public async Task SubmitCreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        exampleSentence.Order = await OrderPicker.PickOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between);
        await harmonyChangeWriter.AddChange(new CreateExampleSentenceChange(exampleSentence, senseId));
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await SubmitCreateExampleSentence(entryId, senseId, exampleSentence, between);
        return await GetExampleSentence(entryId, senseId, exampleSentence.Id) ?? throw NotFoundException.ForType<ExampleSentence>(exampleSentence.Id);
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await GetExampleSentence(repo, entryId, senseId, id);
    }

    // sense first: loading it brings its examples along, so the second query only runs when the example isn't there
    private static async Task<ExampleSentence?> GetExampleSentence(MiniLcmRepository repo, Guid entryId, Guid senseId, Guid id)
    {
        var sense = await repo.GetSense(senseId);
        if (sense is not null)
        {
            CrdtSenseApi.VerifySenseBelongsToEntry(entryId, sense);
            var owned = sense.ExampleSentences.FirstOrDefault(e => e.Id == id);
            if (owned is not null) return owned;
        }
        var exampleSentence = await repo.GetExampleSentence(id);
        if (exampleSentence is null) return null;
        if (exampleSentence.SenseId != senseId) throw ParentMismatchException.ForType<ExampleSentence>(id, senseId, exampleSentence.SenseId);
        return exampleSentence;
    }

    public async Task SubmitUpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        await harmonyChangeWriter.AddChange(new JsonPatchExampleSentenceChange(exampleSentenceId, update.Patch));
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        await SubmitUpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
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

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (kind == MoveKind.Reorder)
        {
            // see MoveSense
            _ = await GetExampleSentence(repo, entryId, senseId, exampleId) ?? throw NotFoundException.ForType<ExampleSentence>(exampleId);
            await harmonyChangeWriter.AddChange(new Changes.SetOrderChange<ExampleSentence>(exampleId, await PickExampleOrder(repo, senseId, between)));
            return;
        }
        if (!await repo.ExampleSentences.AnyAsyncEF(e => e.Id == exampleId)) throw NotFoundException.ForType<ExampleSentence>(exampleId);
        var targetSense = await repo.GetSense(senseId) ?? throw NotFoundException.ForType<Sense>(senseId);
        CrdtSenseApi.VerifySenseBelongsToEntry(entryId, targetSense);
        await harmonyChangeWriter.AddChange(new MoveExampleSentenceToSenseChange(exampleId, senseId, await PickExampleOrder(repo, senseId, between)));
    }

    public async Task SubmitMoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (kind == MoveKind.Reorder)
        {
            // the example is gone or was reparented on this side: the reorder is moot, skip it
            var example = await repo.GetExampleSentence(exampleSentenceId);
            if (example is null || example.SenseId != senseId) return;
            await harmonyChangeWriter.AddChange(new Changes.SetOrderChange<ExampleSentence>(exampleSentenceId, await PickExampleOrder(repo, senseId, position)));
            return;
        }
        // no target checks: a reparented target sense is fine (the example follows it), and so is a deleted one
        // (the move change then deletes the example, delete wins)
        await harmonyChangeWriter.AddChange(new MoveExampleSentenceToSenseChange(exampleSentenceId, senseId, await PickExampleOrder(repo, senseId, position)));
    }

    private static async Task<double> PickExampleOrder(MiniLcmRepository repo, Guid senseId, BetweenPosition between)
    {
        return await OrderPicker.PickOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between);
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

    public async Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        if (translation.Id == Guid.Empty) translation.Id = Guid.NewGuid();
        await harmonyChangeWriter.AddChange(new AddTranslationChange(exampleSentenceId, translation));
    }

    public async Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        await harmonyChangeWriter.AddChange(new RemoveTranslationChange(exampleSentenceId, translationId));
    }

    public async Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        var jsonPatch = update.Patch;
        await harmonyChangeWriter.AddChange(new UpdateTranslationChange(exampleSentenceId, translationId, jsonPatch));
    }

    [Obsolete($"Use {nameof(AddTranslation)} instead")]
    public async Task SetFirstTranslationIds(IDictionary<Guid, Guid> exampleSentenceIdToTranslationId)
    {
        var changes = exampleSentenceIdToTranslationId
            .Select(kv => GetSetFirstTranslationIdChange(kv.Key, kv.Value));
        await harmonyChangeWriter.AddChanges(changes);

        static SetFirstTranslationIdChange GetSetFirstTranslationIdChange(Guid exampleSentenceId, Guid translationId)
        {
            // When calling this, the first translation of the relevant example-sentence should almost definitely
            // be Translation.MissingTranslationId, which the API maps to the example sentence's DefaultFirstTranslationId.
            // However, there are edge cases, which are probably valid. See the comment above the caling code in CrdtRepairs.
            if (Translation.IsMissingTranslationId(translationId)) throw new InvalidOperationException("Cannot set the first translation id to the missing id placeholder");
            // We could also validate that translationId is not the default first translation ID,
            // but it doesn't really matter if it is. It would just be unexpected.
            return new SetFirstTranslationIdChange(exampleSentenceId, translationId);
        }
    }

    #region PictureApi
    public async Task<Picture> CreatePicture(Guid entryId,
        Guid senseId,
        Picture picture,
        BetweenPosition? between = null)
    {
        return await pictureApi.CreatePicture(entryId, senseId, picture, between);
    }

    public async Task<Picture?> GetPicture(Guid entryId, Guid senseId, Guid id)
    {
        return await pictureApi.GetPicture(entryId, senseId, id);
    }

    public async Task SubmitUpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        await pictureApi.SubmitUpdatePicture(entryId, senseId, pictureId, update);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        return await pictureApi.UpdatePicture(entryId, senseId, pictureId, update);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi? api = null)
    {
        return await pictureApi.UpdatePicture(entryId, senseId, before, after, api ?? this);
    }

    public async Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition between)
    {
        await pictureApi.MovePicture(entryId, senseId, pictureId, between);
    }

    public async Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        await pictureApi.DeletePicture(entryId, senseId, pictureId);
    }
    #endregion

    #region MediaApi
    public async Task<ReadFileResponse> GetFileStream(MediaUri mediaUri, bool downloadIfMissing = true)
    {
        return await mediaApi.GetFileStream(mediaUri, downloadIfMissing);
    }

    public async Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        return await mediaApi.SaveFile(stream, metadata);
    }
    #endregion

    #region CustomViewApi
    public IAsyncEnumerable<CustomView> GetCustomViews()
    {
        return customViewApi.GetCustomViews();
    }

    public async Task<CustomView?> GetCustomView(Guid id)
    {
        return await customViewApi.GetCustomView(id);
    }

    public async Task<CustomView> CreateCustomView(CustomView customView)
    {
        return await customViewApi.CreateCustomView(customView);
    }

    public async Task<CustomView> UpdateCustomView(CustomView customView)
    {
        return await customViewApi.UpdateCustomView(customView);
    }

    public async Task DeleteCustomView(Guid id)
    {
        await customViewApi.DeleteCustomView(id);
    }
    #endregion

    #region CommentApi
    public IAsyncEnumerable<CommentThread> GetCommentThreads(SubjectType subjectType, Guid subjectId, bool includeComments = false)
    {
        return commentApi.GetCommentThreads(subjectType, subjectId, includeComments);
    }

    public async Task<CommentThread?> GetCommentThread(Guid id)
    {
        return await commentApi.GetCommentThread(id);
    }

    public IAsyncEnumerable<UserComment> GetUserComments(Guid threadId)
    {
        return commentApi.GetUserComments(threadId);
    }

    public async Task<UserComment?> GetUserComment(Guid id)
    {
        return await commentApi.GetUserComment(id);
    }

    public IAsyncEnumerable<UserComment> GetUnreadComments(Guid? threadId = null)
    {
        return commentApi.GetUnreadComments(threadId);
    }

    public IAsyncEnumerable<UserComment> GetUnreadCommentsForSubject(SubjectType subjectType, Guid subjectId)
    {
        return commentApi.GetUnreadCommentsForSubject(subjectType, subjectId);
    }

    public async Task<int> CountUnreadComments(Guid? threadId = null)
    {
        return await commentApi.CountUnreadComments(threadId);
    }

    public async Task<CommentThread> CreateCommentThread(CommentThread thread, UserComment firstComment)
    {
        return await commentApi.CreateCommentThread(thread, firstComment);
    }

    public async Task<UserComment> AddUserComment(Guid threadId, UserComment comment)
    {
        return await commentApi.AddUserComment(threadId, comment);
    }

    public async Task<UserComment> EditUserComment(Guid commentId, string text)
    {
        return await commentApi.EditUserComment(commentId, text);
    }

    public async Task<CommentThread> SetCommentThreadStatus(Guid threadId, ThreadStatus status)
    {
        return await commentApi.SetCommentThreadStatus(threadId, status);
    }

    public async Task DeleteUserComment(Guid commentId)
    {
        await commentApi.DeleteUserComment(commentId);
    }

    public async Task DeleteCommentThread(Guid threadId)
    {
        await commentApi.DeleteCommentThread(threadId);
    }

    public async Task MarkCommentRead(Guid commentId)
    {
        await commentApi.MarkCommentRead(commentId);
    }

    public async Task MarkCommentThreadUnread(Guid threadId)
    {
        await commentApi.MarkCommentThreadUnread(threadId);
    }

    public async Task MarkCommentThreadRead(Guid threadId)
    {
        await commentApi.MarkCommentThreadRead(threadId);
    }

    public async Task MarkAllCommentsRead()
    {
        await commentApi.MarkAllCommentsRead();
    }
    #endregion

    public void Dispose()
    {
    }
}
