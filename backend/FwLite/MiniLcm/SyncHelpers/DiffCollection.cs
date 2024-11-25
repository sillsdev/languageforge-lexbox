using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

public static class DiffCollection
{
    /// <summary>
    /// Diffs a list, for new items calls add, it will then call update for the item returned from the add, using that as the before item for the replace call
    /// </summary>
    /// <param name="api"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="identity"></param>
    /// <param name="add">api, value, return value to be used as the before item for the replace call</param>
    /// <param name="remove"></param>
    /// <param name="replace">api, before, after is the parameter order</param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <returns></returns>
    public static async Task<int> DiffAddThenUpdate<T, TId>(
        IMiniLcmApi api,
        IList<T> before,
        IList<T> after,
        Func<T, TId> identity,
        Func<IMiniLcmApi, T, Task<T>> add,
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

        var postAddUpdates = new List<(T created, T after)>(afterEntriesDict.Values.Count);
        foreach (var value in afterEntriesDict.Values)
        {
            changes++;
            postAddUpdates.Add((await add(api, value), value));
        }
        foreach ((T createdItem, T afterItem) in postAddUpdates)
        {
            //todo this may do a lot more work than it needs to, eg sense will be created during add, but they will be checked again here when we know they didn't change
            await replace(api, createdItem, afterItem);
        }

        return changes;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="api"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="identity"></param>
    /// <param name="add"></param>
    /// <param name="remove"></param>
    /// <param name="replace">api, before, after is the parameter order</param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <returns></returns>
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
