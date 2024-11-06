using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class EntrySync
{
    public static async Task<int> Sync(Entry[] afterEntries,
        Entry[] beforeEntries,
        IMiniLcmApi api)
    {
        Func<IMiniLcmApi, Entry, Task<int>> add = static async (api, afterEntry) =>
        {
            await api.CreateEntry(afterEntry);
            return 1;
        };
        Func<IMiniLcmApi, Entry, Task<int>> remove = static async (api, beforeEntry) =>
        {
            await api.DeleteEntry(beforeEntry.Id);
            return 1;
        };
        Func<IMiniLcmApi, Entry, Entry, Task<int>> replace = static async (api, beforeEntry, afterEntry) => await Sync(afterEntry, beforeEntry, api);
        return await DiffCollection.Diff(api, beforeEntries, afterEntries, add, remove, replace);
    }

    public static async Task<int> Sync(Entry afterEntry, Entry beforeEntry, IMiniLcmApi api)
    {
        var updateObjectInput = EntryDiffToUpdate(beforeEntry, afterEntry);
        if (updateObjectInput is not null) await api.UpdateEntry(afterEntry.Id, updateObjectInput);
        var changes = await SensesSync(afterEntry.Id, afterEntry.Senses, beforeEntry.Senses, api);

        changes += await Sync(afterEntry.Components, beforeEntry.Components, api);
        changes += await Sync(afterEntry.ComplexForms, beforeEntry.ComplexForms, api);
        changes += await Sync(afterEntry.Id, afterEntry.ComplexFormTypes, beforeEntry.ComplexFormTypes, api);
        return changes + (updateObjectInput is null ? 0 : 1);
    }

    private static async Task<int> Sync(Guid entryId,
        IList<ComplexFormType> afterComplexFormTypes,
        IList<ComplexFormType> beforeComplexFormTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            beforeComplexFormTypes,
            afterComplexFormTypes,
            complexFormType => complexFormType.Id,
            async (api, afterComplexFormType) =>
            {
                await api.AddComplexFormType(entryId, afterComplexFormType.Id);
                return 1;
            },
            async (api, beforeComplexFormType) =>
            {
                await api.RemoveComplexFormType(entryId, beforeComplexFormType.Id);
                return 1;
            },
            //do nothing, complex form types are not editable, ignore any changes to them here
            static (api, beforeComplexFormType, afterComplexFormType) => Task.FromResult(0));
    }

    private static async Task<int> Sync(IList<ComplexFormComponent> afterComponents, IList<ComplexFormComponent> beforeComponents, IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            beforeComponents,
            afterComponents,
            //we can't use the ID as there's none defined by Fw so it won't work as a sync key
            component => (component.ComplexFormEntryId, component.ComponentEntryId, component.ComponentSenseId),
            static async (api, afterComponent) =>
            {
                //change id, since we're not using the id as the key for this collection
                //the id may be the same, which is not what we want here
                afterComponent.Id = Guid.NewGuid();
                await api.CreateComplexFormComponent(afterComponent);
                return 1;
            },
            static async (api, beforeComponent) =>
            {
                await api.DeleteComplexFormComponent(beforeComponent);
                return 1;
            },
            static (api, beforeComponent, afterComponent) =>
            {
                if (beforeComponent.ComplexFormEntryId == afterComponent.ComplexFormEntryId &&
                    beforeComponent.ComponentEntryId == afterComponent.ComponentEntryId &&
                    beforeComponent.ComponentSenseId == afterComponent.ComponentSenseId)
                {
                    return Task.FromResult(0);
                }
                throw new InvalidOperationException($"changing complex form components is not supported, they should just be deleted and recreated");
            }
        );
    }

    private static async Task<int> SensesSync(Guid entryId,
        IList<Sense> afterSenses,
        IList<Sense> beforeSenses,
        IMiniLcmApi api)
    {
        Func<IMiniLcmApi, Sense, Task<int>> add = async (api, afterSense) =>
        {
            await api.CreateSense(entryId, afterSense);
            return 1;
        };
        Func<IMiniLcmApi, Sense, Task<int>> remove = async (api, beforeSense) =>
        {
            await api.DeleteSense(entryId, beforeSense.Id);
            return 1;
        };
        Func<IMiniLcmApi, Sense, Sense, Task<int>> replace = async (api, beforeSense, afterSense) => await SenseSync.Sync(entryId, afterSense, beforeSense, api);
        return await DiffCollection.Diff(api, beforeSenses, afterSenses, add, remove, replace);
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
}
