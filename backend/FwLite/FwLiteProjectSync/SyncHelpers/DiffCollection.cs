using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.SyncHelpers;

public static class DiffCollection
{
    public static async Task<int> Diff<T, TId>(
        IMiniLcmApi api,
        IList<T> before,
        IList<T> after,
        Func<T, TId> identity,
        Func<IMiniLcmApi, T, Task<int>> add,
        Func<IMiniLcmApi, T, Task<int>> remove,
        Func<IMiniLcmApi, T, T, Task<int>> replace) where TId : notnull
    {
        var changes = 0;
        var afterEntriesDict = after.ToDictionary(identity);
        foreach (var beforeEntry in before)
        {
            if (afterEntriesDict.TryGetValue(identity(beforeEntry), out var afterEntry))
            {
                changes += await replace(api, beforeEntry, afterEntry);
            }
            else
            {
                changes += await remove(api, beforeEntry);
            }

            afterEntriesDict.Remove(identity(beforeEntry));
        }

        foreach (var value in afterEntriesDict.Values)
        {
            changes += await add(api, value);
        }

        return changes;
    }
    public static async Task<int> Diff<T>(
        IMiniLcmApi api,
        IList<T> before,
        IList<T> after,
        Func<IMiniLcmApi, T, Task<int>> add,
        Func<IMiniLcmApi, T, Task<int>> remove,
        Func<IMiniLcmApi, T, T, Task<int>> replace) where T : IObjectWithId
    {
        return await Diff(api, before, after, t => t.Id, add, remove, replace);
    }

    public static async Task<int> Diff<T>(
        IMiniLcmApi api,
        IList<T> before,
        IList<T> after,
        Func<IMiniLcmApi, T, Task> add,
        Func<IMiniLcmApi, T, Task> remove,
        Func<IMiniLcmApi, T, T, Task<int>> replace) where T : IObjectWithId
    {
        return await Diff(api,
            before,
            after,
            async (api, entry) =>
            {
                await add(api, entry);
                return 1;
            },
            async (api, entry) =>
            {
                await remove(api, entry);
                return 1;
            },
            async (api, beforeEntry, afterEntry) => await replace(api, beforeEntry, afterEntry));
    }
}
