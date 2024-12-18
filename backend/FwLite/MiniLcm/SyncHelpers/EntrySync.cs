using MiniLcm.Exceptions;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class EntrySync
{
    public static async Task<int> Sync(Entry[] afterEntries,
        Entry[] beforeEntries,
        IMiniLcmApi api)
    {
        return await DiffCollection.DiffAddThenUpdate(beforeEntries, afterEntries, new EntriesDiffApi(api));
    }

    public static async Task<int> Sync(Entry afterEntry, Entry beforeEntry, IMiniLcmApi api)
    {
        try
        {
            var updateObjectInput = EntryDiffToUpdate(beforeEntry, afterEntry);
            if (updateObjectInput is not null) await api.UpdateEntry(afterEntry.Id, updateObjectInput);
            var changes = await SensesSync(afterEntry.Id, afterEntry.Senses, beforeEntry.Senses, api);

            changes += await Sync(afterEntry.Components, beforeEntry.Components, api);
            changes += await Sync(afterEntry.ComplexForms, beforeEntry.ComplexForms, api);
            changes += await Sync(afterEntry.Id, afterEntry.ComplexFormTypes, beforeEntry.ComplexFormTypes, api);
            return changes + (updateObjectInput is null ? 0 : 1);
        }
        catch (Exception e)
        {
            throw new SyncObjectException($"Failed to sync entry {afterEntry}", e);
        }
    }

    private static async Task<int> Sync(Guid entryId,
        IList<ComplexFormType> afterComplexFormTypes,
        IList<ComplexFormType> beforeComplexFormTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComplexFormTypes,
            afterComplexFormTypes,
            new ComplexFormTypesDiffApi(api, entryId));
    }

    private static async Task<int> Sync(IList<ComplexFormComponent> afterComponents, IList<ComplexFormComponent> beforeComponents, IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComponents,
            afterComponents,
            new ComplexFormComponentsDiffApi(api)
        );
    }

    private static async Task<int> SensesSync(Guid entryId,
        IList<Sense> afterSenses,
        IList<Sense> beforeSenses,
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
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Entry>(patchDocument);
    }

    private class EntriesDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<Entry>
    {
        public override async Task<(int, Entry)> AddAndGet(Entry afterEntry)
        {
            //create each entry without components.
            //After each entry is created, then replace will be called to create those components
            var entryWithoutEntryRefs = afterEntry.WithoutEntryRefs();
            var changes = await Add(entryWithoutEntryRefs);
            return (changes, entryWithoutEntryRefs);
        }

        public override async Task<int> Add(Entry afterEntry)
        {
            await api.CreateEntry(afterEntry);
            return 1;
        }

        public override async Task<int> Remove(Entry entry)
        {
            await api.DeleteEntry(entry.Id);
            return 1;
        }

        public override Task<int> Replace(Entry before, Entry after)
        {
            return Sync(after, before, api);
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

    private class ComplexFormComponentsDiffApi(IMiniLcmApi api) : CollectionDiffApi<ComplexFormComponent, (Guid, Guid, Guid?)>
    {
        public override (Guid, Guid, Guid?) GetId(ComplexFormComponent component)
        {
            //we can't use the ID as there's none defined by Fw so it won't work as a sync key
            return (component.ComplexFormEntryId, component.ComponentEntryId, component.ComponentSenseId);
        }

        public override async Task<int> Add(ComplexFormComponent afterComplexFormType)
        {
            //change id, since we're not using the id as the key for this collection
            //the id may be the same, which is not what we want here
            afterComplexFormType.Id = Guid.NewGuid();
            try
            {
                await api.CreateComplexFormComponent(afterComplexFormType);
            }
            catch (NotFoundException)
            {
                //this can happen if the entry was deleted, so we can just ignore it
            }
            return 1;
        }

        public override async Task<int> Remove(ComplexFormComponent beforeComplexFormType)
        {
            await api.DeleteComplexFormComponent(beforeComplexFormType);
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
            return SenseSync.Sync(entryId, after, before, api);
        }
    }
}
