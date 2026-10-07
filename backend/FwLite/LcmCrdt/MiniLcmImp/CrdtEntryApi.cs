using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.FullTextSearch;
using LcmCrdt.Harmony;
using LcmCrdt.Objects;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;

namespace LcmCrdt.MiniLcmImp;

public class CrdtEntryApi(
    MiniLcmRepositoryFactory repoFactory,
    HarmonyChangeWriter harmonyChangeWriter,
    ILogger<CrdtEntryApi> logger,
    EntrySearchService? entrySearchService = null)
{
    // Flush threshold (in accumulated IChange records) for BulkCreateEntries; internal so tests can force multi-batch behavior.
    internal int BulkCreateBatchSize { get; set; } = 1000;

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

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi api)
    {
        await EntrySync.SyncFull(before, after, api);
        var updatedEntry = await GetEntry(after.Id) ?? throw NotFoundException.ForType<Entry>(after.Id);
        return updatedEntry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<Entry>(id));
    }
}
