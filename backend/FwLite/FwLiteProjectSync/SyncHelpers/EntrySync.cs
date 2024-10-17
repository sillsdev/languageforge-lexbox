using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.SyncHelpers;

public static class EntrySync
{
    public static async Task<int> Sync(Entry[] currentEntries,
        Entry[] previousEntries,
        IMiniLcmApi api)
    {
        Func<IMiniLcmApi, Entry, Task<int>> add = static async (api, currentEntry) =>
        {
            await api.CreateEntry(currentEntry);
            return 1;
        };
        Func<IMiniLcmApi, Entry, Task<int>> remove = static async (api, previousEntry) =>
        {
            await api.DeleteEntry(previousEntry.Id);
            return 1;
        };
        Func<IMiniLcmApi, Entry, Entry, Task<int>> replace = static async (api, previousEntry, currentEntry) => await Sync(currentEntry, previousEntry, api);
        return await DiffCollection.Diff(api, previousEntries, currentEntries, add, remove, replace);
    }

    public static async Task<int> Sync(Entry currentEntry, Entry previousEntry, IMiniLcmApi api)
    {
        var updateObjectInput = EntryDiffToUpdate(previousEntry, currentEntry);
        if (updateObjectInput is not null) await api.UpdateEntry(currentEntry.Id, updateObjectInput);
        var changes = await SensesSync(currentEntry.Id, currentEntry.Senses, previousEntry.Senses, api);
        return changes + (updateObjectInput is null ? 0 : 1);
    }

    private static async Task<int> SensesSync(Guid entryId,
        IList<Sense> currentSenses,
        IList<Sense> previousSenses,
        IMiniLcmApi api)
    {
        Func<IMiniLcmApi, Sense, Task<int>> add = async (api, currentSense) =>
        {
            await api.CreateSense(entryId, currentSense);
            return 1;
        };
        Func<IMiniLcmApi, Sense, Task<int>> remove = async (api, previousSense) =>
        {
            await api.DeleteSense(entryId, previousSense.Id);
            return 1;
        };
        Func<IMiniLcmApi, Sense, Sense, Task<int>> replace = async (api, previousSense, currentSense) => await SenseSync.Sync(entryId, currentSense, previousSense, api);
        return await DiffCollection.Diff(api, previousSenses, currentSenses, add, remove, replace);
    }

    public static UpdateObjectInput<Entry>? EntryDiffToUpdate(Entry previousEntry, Entry currentEntry)
    {
        JsonPatchDocument<Entry> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.LexemeForm), previousEntry.LexemeForm, currentEntry.LexemeForm));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.CitationForm), previousEntry.CitationForm, currentEntry.CitationForm));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.Note), previousEntry.Note, currentEntry.Note));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Entry>(nameof(Entry.LiteralMeaning), previousEntry.LiteralMeaning, currentEntry.LiteralMeaning));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Entry>(patchDocument);
    }
}
