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
        Func<IMiniLcmApi, Entry, Task<Entry>> add = static async (api, afterEntry) =>
        {
            //create each entry without components.
            //After each entry is created, then replace will be called to create those components
            var entryWithoutEntryRefs = afterEntry.WithoutEntryRefs();
            await api.CreateEntry(entryWithoutEntryRefs);
            return entryWithoutEntryRefs;
        };
        Func<IMiniLcmApi, Entry, Task<int>> remove = static async (api, beforeEntry) =>
        {
            await api.DeleteEntry(beforeEntry.Id);
            return 1;
        };
        Func<IMiniLcmApi, Entry, Entry, Task<int>> replace = static async (api, beforeEntry, afterEntry) => await Sync(afterEntry, beforeEntry, api);
        return await DiffCollection.DiffAddThenUpdate(api, beforeEntries, afterEntries, entry => entry.Id, add, remove, replace);
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
                try
                {
                    await api.CreateComplexFormComponent(afterComponent);
                }
                catch (NotFoundException)
                {
                    //this can happen if the entry was deleted, so we can just ignore it
                }
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

    private static (Guid? beforeId, Guid? afterId) GetSurroundingIds<T>(int i, IList<T> current, List<Guid> stable) where T : class, IOrderable
    {
        T? beforeEntity = null;
        T? afterEntity = null;
        for (var j = i - 1; j >= 0; j--)
        {
            if (stable.Contains(current[j].Id))
            {
                beforeEntity = current[j];
                break;
            }
        }
        for (var j = i + 1; j < stable.Count; j++)
        {
            if (stable.Contains(current[j].Id))
            {
                afterEntity = current[j];
                break;
            }
        }
        return (beforeEntity?.Id, afterEntity?.Id);
    }
    private static async Task<int> SensesSync(Guid entryId,
        IList<Sense> afterSenses,
        IList<Sense> beforeSenses,
        IMiniLcmApi api)
    {
        var prevMin = beforeSenses.Min(s => s.Order);
        Func<IMiniLcmApi, Sense, int, List<Guid>, Task<int>> add = async (api, sense, to, stable) =>
        {
            var (beforeId, afterId) = GetSurroundingIds(to, afterSenses, stable);
            await api.CreateSense(entryId, sense, beforeId, afterId);
            stable.Add(sense.Id);
            return 1;
        };
        Func<IMiniLcmApi, Sense, int, List<Guid>, Task<int>> move = async (api, sense, to, stable) =>
        {
            var (beforeId, afterId) = GetSurroundingIds(to, afterSenses, stable);
            await api.MoveSense(entryId, sense, beforeId, afterId);
            stable.Add(sense.Id);
            return 1;
        };
        Func<IMiniLcmApi, Sense, Task<int>> remove = async (api, sense) =>
        {
            await api.DeleteSense(entryId, sense.Id);
            return 1;
        };
        Func<IMiniLcmApi, Sense, Sense, Task<int>> replace = async (api, beforeSense, afterSense) => await SenseSync.Sync(entryId, afterSense, beforeSense, api);
        return await DiffCollection.DiffOrderable(api, beforeSenses, afterSenses, add, remove, move, replace);
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
