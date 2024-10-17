using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.SyncHelpers;

public static class DiffCollection
{
    public static async Task<int> Diff<T>(
        IMiniLcmApi api,
        IList<T> previous,
        IList<T> current,
        Func<IMiniLcmApi, T, Task<int>> add,
        Func<IMiniLcmApi, T, Task<int>> remove,
        Func<IMiniLcmApi, T, T, Task<int>> replace) where T : IObjectWithId
    {
        var changes = 0;
        var currentEntriesDict = current.ToDictionary(entry => entry.Id);
        foreach (var previousEntry in previous)
        {
            if (currentEntriesDict.TryGetValue(previousEntry.Id, out var currentEntry))
            {
                changes += await replace(api, previousEntry, currentEntry);
            }
            else
            {
                changes += await remove(api, previousEntry);
            }

            currentEntriesDict.Remove(previousEntry.Id);
        }

        foreach (var value in currentEntriesDict.Values)
        {
            changes += await add(api, value);
        }

        return changes;
    }
}
