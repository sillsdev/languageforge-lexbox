using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.SyncHelpers;

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

        changes += await DiffCollection.Diff(api,
            beforeEntry.Components,
            afterEntry.Components,
            component => (component.ComplexFormEntryId, component.ComponentEntryId, component.ComponentSenseId),
            static async (api, afterComponent) =>
            {
                await api.CreateComplexFormComponent(afterComponent);
                return 1;
            },
            static async (api, beforeComponent) =>
            {
                await api.DeleteComplexFormComponent(beforeComponent);
                return 1;
            },
            static async (api, beforeComponent, afterComponent) =>
            {
                if (beforeComponent.ComplexFormEntryId == afterComponent.ComplexFormEntryId &&
                    beforeComponent.ComponentEntryId == afterComponent.ComponentEntryId &&
                    beforeComponent.ComponentSenseId == afterComponent.ComponentSenseId)
                {
                    return 0;
                }
                await api.ReplaceComplexFormComponent(beforeComponent, afterComponent);
                return 1;
            }
        );
        return changes + (updateObjectInput is null ? 0 : 1);
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
