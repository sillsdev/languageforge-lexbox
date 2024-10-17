using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.SyncHelpers;

public static class DiffCollection
{
    public static async Task<int> Diff<T>(
        IMiniLcmApi api,
        IList<T> before,
        IList<T> after,
        Func<IMiniLcmApi, T, Task<int>> add,
        Func<IMiniLcmApi, T, Task<int>> remove,
        Func<IMiniLcmApi, T, T, Task<int>> replace) where T : IObjectWithId
    {
        var changes = 0;
        var afterEntriesDict = after.ToDictionary(entry => entry.Id);
        foreach (var beforeEntry in before)
        {
            if (afterEntriesDict.TryGetValue(beforeEntry.Id, out var afterEntry))
            {
                changes += await replace(api, beforeEntry, afterEntry);
            }
            else
            {
                changes += await remove(api, beforeEntry);
            }

            afterEntriesDict.Remove(beforeEntry.Id);
        }

        foreach (var value in afterEntriesDict.Values)
        {
            changes += await add(api, value);
        }

        return changes;
    }
}
