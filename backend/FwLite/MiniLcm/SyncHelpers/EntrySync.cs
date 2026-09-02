using MiniLcm.Exceptions;
using MiniLcm.Models;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace MiniLcm.SyncHelpers;

public static class EntrySync
{
    public static async Task<int> SyncFull(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        var (changes, addedEntries) = await SyncWithoutComplexFormsAndComponents(beforeEntries, afterEntries, api);
        var updatedBeforeEntries = beforeEntries.Where(before => afterEntries.Any(after => after.Id == before.Id));
        changes += await SyncComplexFormsAndComponents([.. updatedBeforeEntries, .. addedEntries], afterEntries, api);
        return changes;
    }

    public static async Task<(int Changes, ICollection<Entry> Added)> SyncWithoutComplexFormsAndComponents(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        var context = new SyncContext(beforeEntries, afterEntries);
        var (changes, added) = await DiffCollection.DiffAndGetAdded(beforeEntries, afterEntries, new EntriesDiffApi(api, context), context.Entries);
        changes += await context.DeferredDeletes.DeleteAll();
        return (changes, added);
    }

    /// <summary>
    /// Syncs only the complex forms and components of the before and after entries.
    /// <exception cref="InvalidOperationException">When the before and after entries do not match.</exception>
    /// </summary>
    public static async Task<int> SyncComplexFormsAndComponents(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(beforeEntries, afterEntries,
            new ObjectWithIdCollectionReplaceDiffApi<Entry>(
                (before, after) => SyncComplexFormsAndComponents(before, after, api)),
            MoveContext<Entry, Guid>.Empty);
    }

    public static async Task<int> SyncFull(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api)
    {
        return await SyncFull([beforeEntry], [afterEntry], api);
    }

    public static async Task<int> SyncWithoutComplexFormsAndComponents(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api)
    {
        var context = new SyncContext([beforeEntry], [afterEntry]);
        var changes = await SyncWithoutComplexFormsAndComponents(beforeEntry, afterEntry, api, context);
        changes += await context.DeferredDeletes.DeleteAll();
        return changes;
    }

    // callers passing a context own it: they must drain context.DeferredDeletes after the walk
    private static async Task<int> SyncWithoutComplexFormsAndComponents(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api, SyncContext context)
    {
        try
        {
            var updateObjectInput = EntryDiffToUpdate(beforeEntry, afterEntry);
            if (updateObjectInput is not null) await api.SubmitUpdateEntry(afterEntry.Id, updateObjectInput);
            var changes = await SensesSync(afterEntry.Id, beforeEntry.Senses, afterEntry.Senses, api, context);
            changes += await Sync(afterEntry.Id, beforeEntry.ComplexFormTypes, afterEntry.ComplexFormTypes, api);
            changes += await SyncPublications(afterEntry.Id, beforeEntry.PublishIn, afterEntry.PublishIn, api);
            return changes + (updateObjectInput is null ? 0 : 1);
        }
        catch (Exception e)
        {
            throw new SyncObjectException($"Failed to sync entry {afterEntry}", e);
        }
    }

    public static async Task<int> SyncComplexFormsAndComponents(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api)
    {
        try
        {
            var changes = 0;
            changes += await SyncComplexFormComponents(beforeEntry.Components, afterEntry.Components, api);
            changes += await SyncComplexForms(beforeEntry.ComplexForms, afterEntry.ComplexForms, api);
            return changes;
        }
        catch (Exception e)
        {
            throw new SyncObjectException($"Failed to sync complex forms and components of entry {afterEntry}", e);
        }
    }

    private static async Task<int> SyncPublications(Guid entryId,
        IList<Publication> beforePublications,
        IList<Publication> afterPublications,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforePublications,
            afterPublications,
            new PublicationsDiffApi(api, entryId),
            MoveContext<Publication, Guid>.Empty);
    }

    private static async Task<int> Sync(Guid entryId,
        IList<ComplexFormType> beforeComplexFormTypes,
        IList<ComplexFormType> afterComplexFormTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComplexFormTypes,
            afterComplexFormTypes,
            new ComplexFormTypesDiffApi(api, entryId),
            MoveContext<ComplexFormType, Guid>.Empty);
    }

    private static async Task<int> SyncComplexFormComponents(IList<ComplexFormComponent> beforeComponents, IList<ComplexFormComponent> afterComponents, IMiniLcmApi api)
    {
        return await DiffCollection.DiffOrderable(
            beforeComponents,
            afterComponents,
            new ComplexFormComponentsDiffApi(api),
            MoveContext<ComplexFormComponent, (Guid, Guid, Guid?)>.Empty
        );
    }

    private static async Task<int> SyncComplexForms(IList<ComplexFormComponent> beforeComponents, IList<ComplexFormComponent> afterComponents, IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComponents,
            afterComponents,
            new ComplexFormsDiffApi(api),
            MoveContext<ComplexFormComponent, (Guid, Guid, Guid?)>.Empty
        );
    }

    private static async Task<int> SensesSync(Guid entryId,
        IList<Sense> beforeSenses,
        IList<Sense> afterSenses,
        IMiniLcmApi api,
        SyncContext context)
    {
        return await DiffCollection.DiffOrderable(beforeSenses, afterSenses, new SensesDiffApi(api, entryId, context), context.Senses);
    }

    public static UpdateObjectInput<Entry>? EntryDiffToUpdate(Entry beforeEntry, Entry afterEntry)
    {
        JsonPatchDocument<Entry> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.LexemeForm), beforeEntry.LexemeForm, afterEntry.LexemeForm));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.CitationForm), beforeEntry.CitationForm, afterEntry.CitationForm));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.Note), beforeEntry.Note, afterEntry.Note));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.LiteralMeaning), beforeEntry.LiteralMeaning, afterEntry.LiteralMeaning));
        if (beforeEntry.MorphType != afterEntry.MorphType)
            patchDocument.Operations.Add(new Operation<Entry>("replace", $"/{nameof(Entry.MorphType)}", null, afterEntry.MorphType));
        if (beforeEntry.HomographNumber != afterEntry.HomographNumber)
            patchDocument.Operations.Add(new Operation<Entry>("replace", $"/{nameof(Entry.HomographNumber)}", null, afterEntry.HomographNumber));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Entry>(patchDocument);
    }

    private class EntriesDiffApi(IMiniLcmApi api, SyncContext context) : ObjectWithIdCollectionDiffApi<Entry>
    {
        public override async Task<(int, Entry)> AddAndGet(Entry afterEntry)
        {
            context.ThrowIfCreatingMovedChildren(afterEntry);
            Entry addedEntry;
            if (context.HasChildMovingIn(afterEntry))
            {
                addedEntry = await api.CreateEntry(afterEntry with { Senses = [] }, CreateEntryOptions.WithoutComplexFormsAndComponents);
                await SensesSync(addedEntry.Id, [], afterEntry.Senses, api, context);
                addedEntry = addedEntry with { Senses = afterEntry.Senses };
            }
            else
            {
                addedEntry = await api.CreateEntry(afterEntry, CreateEntryOptions.WithoutComplexFormsAndComponents);
            }
            return (1, addedEntry);
        }

        public override async Task<int> Remove(Entry entry)
        {
            await api.DeleteEntry(entry.Id);
            return 1;
        }

        public override Task<int> Replace(Entry before, Entry after)
        {
            return SyncWithoutComplexFormsAndComponents(before, after, api, context);
        }
    }

    private class ComplexFormTypesDiffApi(IMiniLcmApi api, Guid entryId) : ObjectWithIdCollectionDiffApi<ComplexFormType>
    {
        public override async Task<int> Add(ComplexFormType afterComplexFormType)
        {
            await api.AddComplexFormType(entryId, afterComplexFormType.Id);
            return 1;
        }

        public override async Task<int> Remove(ComplexFormType beforeComplexFormType)
        {
            await api.RemoveComplexFormType(entryId, beforeComplexFormType.Id);
            return 1;
        }

        public override Task<int> Replace(ComplexFormType before, ComplexFormType after)
        {
            return Task.FromResult(0);
        }
    }

    private class PublicationsDiffApi(IMiniLcmApi api, Guid entryId) : ObjectWithIdCollectionDiffApi<Publication>
    {
        public override async Task<int> Add(Publication afterPub)
        {
            await api.AddPublication(entryId, afterPub.Id);
            return 1;
        }

        public override async Task<int> Remove(Publication beforePub)
        {
            await api.RemovePublication(entryId, beforePub.Id);
            return 1;
        }

        public override Task<int> Replace(Publication beforePub, Publication afterPub)
        {
            return Task.FromResult(0);
        }
    }

    private class ComplexFormsDiffApi(IMiniLcmApi api) : CollectionDiffApi<ComplexFormComponent, (Guid, Guid, Guid?)>
    {
        public override (Guid, Guid, Guid?) GetId(ComplexFormComponent component)
        {
            //we can't use the ID as there's none defined by Fw so it won't work as a sync key
            return (component.ComplexFormEntryId, component.ComponentEntryId, component.ComponentSenseId);
        }

        public override async Task<int> Add(ComplexFormComponent after)
        {
            await api.SubmitCreateComplexFormComponent(after);
            return 1;
        }

        public override async Task<int> Remove(ComplexFormComponent before)
        {
            await api.DeleteComplexFormComponent(before);
            return 1;
        }

        public override Task<int> Replace(ComplexFormComponent beforeComponent, ComplexFormComponent afterComponent)
        {
            if (beforeComponent.ComplexFormEntryId == afterComponent.ComplexFormEntryId &&
                beforeComponent.ComponentEntryId == afterComponent.ComponentEntryId &&
                beforeComponent.ComponentSenseId == afterComponent.ComponentSenseId)
            {
                return Task.FromResult(0);
            }
            throw new InvalidOperationException($"changing complex form components is not supported, they should just be deleted and recreated");
        }
    }

    private class ComplexFormComponentsDiffApi(IMiniLcmApi api) : IOrderableCollectionDiffApi<ComplexFormComponent, (Guid, Guid, Guid?)>
    {
        public (Guid, Guid, Guid?) GetId(ComplexFormComponent component)
        {
            // we can't use the ID as there's none defined by Fw so it won't work as a sync key
            return (component.ComplexFormEntryId, component.ComponentEntryId, component.ComponentSenseId);
        }

        public async Task<int> Add(ComplexFormComponent after, BetweenPosition<ComplexFormComponent> between)
        {
            await api.SubmitCreateComplexFormComponent(after, between);
            return 1;
        }

        public async Task<int> Move(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
        {
            await api.SubmitMoveComplexFormComponent(component, between);
            return 1;
        }

        public async Task<int> Remove(ComplexFormComponent before)
        {
            await api.DeleteComplexFormComponent(before);
            return 1;
        }

        public Task<int> Replace(ComplexFormComponent beforeComponent, ComplexFormComponent afterComponent)
        {
            if (beforeComponent.ComplexFormEntryId == afterComponent.ComplexFormEntryId &&
                beforeComponent.ComponentEntryId == afterComponent.ComponentEntryId &&
                beforeComponent.ComponentSenseId == afterComponent.ComponentSenseId)
            {
                return Task.FromResult(0);
            }
            throw new InvalidOperationException($"changing complex form components is not supported, they should just be deleted and recreated");
        }
    }

    private class SensesDiffApi(IMiniLcmApi api, Guid entryId, SyncContext context) : IOrderableCollectionDiffApi<Sense, Guid>
    {
        public Guid GetId(Sense sense)
        {
            return sense.Id;
        }

        public async Task<int> Add(Sense sense, BetweenPosition<Sense> between)
        {
            context.ThrowIfCreatingMovedChildren(sense);
            if (context.HasChildMovingIn(sense))
            {
                var senseWithoutExamples = sense.Copy();
                senseWithoutExamples.ExampleSentences = [];
                await api.SubmitCreateSense(entryId, senseWithoutExamples, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
                return 1 + await ExampleSentenceSync.Sync(entryId, sense.Id, [], sense.ExampleSentences, api, context);
            }
            await api.SubmitCreateSense(entryId, sense, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Move(Sense sense, BetweenPosition<Sense> between)
        {
            await api.MoveSense(entryId, sense.Id, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Remove(Sense sense)
        {
            await api.DeleteSense(entryId, sense.Id);
            return 1;
        }

        public Task<int> Replace(Sense before, Sense after)
        {
            return SenseSync.Sync(entryId, before, after, api, context);
        }
    }
}
