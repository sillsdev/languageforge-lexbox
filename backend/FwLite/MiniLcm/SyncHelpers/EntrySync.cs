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
        var changes = await SyncWithoutComplexFormsAndComponents(beforeEntries, afterEntries, api);
        changes += await SyncComplexFormsAndComponents(beforeEntries, afterEntries, api);
        return changes;
    }

    public static async Task<int> SyncWithoutComplexFormsAndComponents(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(beforeEntries, afterEntries, new EntriesDiffApi(api));
    }

    public static async Task<int> SyncComplexFormsAndComponents(Entry[] beforeEntries,
        Entry[] afterEntries,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(beforeEntries, afterEntries,
            new ObjectWithIdCollectionReplaceOnlyDiffApi<Entry>(
                (before, after) => SyncComplexFormsAndComponents(before, after, api)));
    }

    public static async Task<int> SyncFull(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api)
    {
        var changes = await SyncWithoutComplexFormsAndComponents(beforeEntry, afterEntry, api);
        changes += await SyncComplexFormsAndComponents(beforeEntry, afterEntry, api);
        return changes;
    }

    public static async Task<int> SyncWithoutComplexFormsAndComponents(Entry beforeEntry, Entry afterEntry, IMiniLcmApi api)
    {
        try
        {
            var updateObjectInput = EntryDiffToUpdate(beforeEntry, afterEntry);
            if (updateObjectInput is not null) await api.UpdateEntry(afterEntry.Id, updateObjectInput);
            var changes = await SensesSync(afterEntry.Id, beforeEntry.Senses, afterEntry.Senses, api);
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
            changes += await SyncComplexFormComponents(afterEntry, beforeEntry.Components, afterEntry.Components, api);
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
            new PublicationsDiffApi(api, entryId));
    }

    private static async Task<int> Sync(Guid entryId,
        IList<ComplexFormType> beforeComplexFormTypes,
        IList<ComplexFormType> afterComplexFormTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComplexFormTypes,
            afterComplexFormTypes,
            new ComplexFormTypesDiffApi(api, entryId));
    }

    private static async Task<int> SyncComplexFormComponents(Entry afterEntry, IList<ComplexFormComponent> beforeComponents, IList<ComplexFormComponent> afterComponents, IMiniLcmApi api)
    {
        return await DiffCollection.DiffOrderable(
            beforeComponents,
            afterComponents,
            new ComplexFormComponentsDiffApi(afterEntry, api)
        );
    }

    private static async Task<int> SyncComplexForms(IList<ComplexFormComponent> beforeComponents, IList<ComplexFormComponent> afterComponents, IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComponents,
            afterComponents,
            new ComplexFormsDiffApi(api)
        );
    }

    private static async Task<int> SensesSync(Guid entryId,
        IList<Sense> beforeSenses,
        IList<Sense> afterSenses,
        IMiniLcmApi api)
    {
        return await DiffCollection.DiffOrderable(beforeSenses, afterSenses, new SensesDiffApi(api, entryId));
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
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Entry>(patchDocument);
    }

    private class EntriesDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<Entry>
    {
        public override async Task<int> Add(Entry afterEntry)
        {
            await api.CreateEntry(afterEntry, CreateEntryOptions.WithoutComplexFormsAndComponents);
            return 1;
        }

        public override async Task<int> Remove(Entry entry)
        {
            await api.DeleteEntry(entry.Id);
            return 1;
        }

        public override Task<int> Replace(Entry before, Entry after)
        {
            return SyncWithoutComplexFormsAndComponents(before, after, api);
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
            //We're not using the id as the key for this collection.
            //So, if a client only changed ComplexFormEntryId it would trigger
            //this Add with an id that is already in use. So we need to change it.
            after.Id = Guid.NewGuid();
            try
            {
                await api.CreateComplexFormComponent(after);
            }
            catch (NotFoundException)
            {
                //this can happen if the entry was deleted, so we can just ignore it
            }
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

    private class ComplexFormComponentsDiffApi(Entry afterEntry, IMiniLcmApi api) : IOrderableCollectionDiffApi<ComplexFormComponent>
    {
        public Guid GetId(ComplexFormComponent component)
        {
            // we can't use the ID as there's none defined by Fw so it won't work as a sync key
            return component.ComponentSenseId ?? component.ComponentEntryId;
        }

        private BetweenPosition<ComplexFormComponent> MapBackToEntities(BetweenPosition between)
        {
            var previous = between!.Previous is null ? null : afterEntry.Components.Find(c => GetId(c) == between.Previous);
            var next = between!.Next is null ? null : afterEntry.Components.Find(c => GetId(c) == between.Next);
            return new BetweenPosition<ComplexFormComponent>(previous, next);
        }

        public async Task<int> Add(ComplexFormComponent after, BetweenPosition between)
        {
            var betweenComponents = MapBackToEntities(between);

            //We're not using the id as the key for this collection.
            //So, if a client only changed ComponentEntryId or ComponentSenseId it would trigger
            //this Add with an id that is already in use. So we need to change it.
            after.Id = Guid.NewGuid();
            try
            {
                await api.CreateComplexFormComponent(after, betweenComponents);
            }
            catch (NotFoundException)
            {
                //this can happen if the entry was deleted, so we can just ignore it
            }
            return 1;
        }

        public async Task<int> Move(ComplexFormComponent component, BetweenPosition between)
        {
            var betweenComponents = MapBackToEntities(between);
            await api.MoveComplexFormComponent(component, betweenComponents);
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

    private class SensesDiffApi(IMiniLcmApi api, Guid entryId) : IOrderableCollectionDiffApi<Sense>
    {
        public async Task<int> Add(Sense sense, BetweenPosition between)
        {
            await api.CreateSense(entryId, sense, between);
            return 1;
        }

        public async Task<int> Move(Sense sense, BetweenPosition between)
        {
            await api.MoveSense(entryId, sense.Id, between);
            return 1;
        }

        public async Task<int> Remove(Sense sense)
        {
            await api.DeleteSense(entryId, sense.Id);
            return 1;
        }

        public Task<int> Replace(Sense before, Sense after)
        {
            return SenseSync.Sync(entryId, before, after, api);
        }
    }
}
