using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.Nodes;
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
    public static async Task<int> DiffOrderable<T, TId>(
        IMiniLcmApi api,
        IList<T> before,
        IList<T> after,
        Func<T, TId> identity,
        Func<IMiniLcmApi, T, BetweenPosition, Task<int>> add,
        Func<IMiniLcmApi, T, Task<int>> remove,
        Func<IMiniLcmApi, T, BetweenPosition, Task<int>> move,
        Func<IMiniLcmApi, T, T, Task<int>> replace) where TId : notnull where T : IOrderable
    {
        var positionDiffs = DiffPositions(before, after, identity)
            // Order: Deletes first, then adds and moves from lowest to highest new index
            // important, because new indexes represent final positions, which might not exist yet in the before list
            // With this order, callers don't have to account for potential gaps
            .OrderBy(d => d.To ?? -1).ToList();

        var unstableIndexes = positionDiffs.Select(diff => diff.From).Where(i => i is not null).ToList();
        var stableIds = before.Where((_, i) => !unstableIndexes.Contains(i)).Select(item => item.Id).ToList();

        var changes = 0;
        foreach (var diff in positionDiffs)
        {
            if (diff.From is not null && diff.To is not null)
            {
                var afterEntry = after[diff.To.Value];
                var between = GetSurroundingStableIds(diff.To.Value, after, stableIds);
                changes += await move(api, afterEntry, between);
                stableIds.Add(afterEntry.Id);
            }
            else if (diff.From is not null)
            {
                changes += await remove(api, before[diff.From.Value]);
            }
            else if (diff.To is not null)
            {
                var afterEntry = after[diff.To.Value];
                var between = GetSurroundingStableIds(diff.To.Value, after, stableIds);
                changes += await add(api, afterEntry, between);
                stableIds.Add(afterEntry.Id);
            }
        }

        var afterEntriesDict = after.ToDictionary(identity);
        foreach (var beforeEntry in before)
        {
            if (afterEntriesDict.TryGetValue(identity(beforeEntry), out var afterEntry))
            {
                changes += await replace(api, beforeEntry, afterEntry);
            }
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

    public static async Task<int> DiffOrderable<T>(
        IMiniLcmApi api,
        IList<T> before,
        IList<T> after,
        Func<IMiniLcmApi, T, BetweenPosition, Task<int>> add,
        Func<IMiniLcmApi, T, Task<int>> remove,
        Func<IMiniLcmApi, T, BetweenPosition, Task<int>> move,
        Func<IMiniLcmApi, T, T, Task<int>> replace) where T : IObjectWithId, IOrderable
    {
        return await DiffOrderable(api, before, after, t => (t as IObjectWithId).Id, add, remove, move, replace);
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

    private static BetweenPosition GetSurroundingStableIds<T>(int i, IList<T> current, IReadOnlyList<Guid> stable) where T : IOrderable
    {
        T? beforeEntity = default;
        T? afterEntity = default;
        for (var j = i - 1; j >= 0; j--)
        {
            if (stable.Contains(current[j].Id))
            {
                beforeEntity = current[j];
                break;
            }
        }
        for (var j = i + 1; j < current.Count; j++)
        {
            if (stable.Contains(current[j].Id))
            {
                afterEntity = current[j];
                break;
            }
        }
        return new BetweenPosition
        {
            Before = beforeEntity?.Id,
            After = afterEntity?.Id
        };
    }

    private static IEnumerable<PositionDiff> DiffPositions<T, TId>(
        IList<T> before,
        IList<T> after,
        Func<T, TId> identity)
    {
        var beforeJson = new JsonArray(before.Select(item => JsonValue.Create(identity(item))).ToArray());
        var afterJson = new JsonArray(after.Select(item => JsonValue.Create(identity(item))).ToArray());

        if (JsonDiffPatcher.Diff(beforeJson, afterJson) is not JsonObject result)
        {
            yield break; // no changes
        }

        foreach (var prop in result)
        {
            if (prop.Key == "_t") // diff type
            {
                if (prop.Value!.ToString() != "a") // we're only using the library for diffing shallow arrays
                {
                    throw new InvalidOperationException("Only array diff results are supported");
                }
                continue;
            }
            else if (prop.Key.StartsWith("_")) // e.g _4 => the key represents an old index (removed or moved)
            {
                var previousIndex = int.Parse(prop.Key[1..]);
                var delta = prop.Value!.AsArray();
                var wasMoved = delta[2]!.GetValue<int>() == 3; // 3 is magic number for a move operation
                int? newIndex = wasMoved ? delta[1]!.GetValue<int>() : null; // if it was moved, the new index is at index 1
                if (newIndex is not null)
                {
                    yield return new PositionDiff { From = previousIndex, To = newIndex }; // move
                }
                else
                {
                    yield return new PositionDiff { From = previousIndex }; // remove
                }
            }
            else // e.g. 4 => the key represents a new index
            {
                var addIndex = int.Parse(prop.Key);
                yield return new PositionDiff { To = addIndex }; // add
            }
        }

    }

    private class PositionDiff
    {
        public int? From { get; init; }
        public int? To { get; init; }
    }
}

public class BetweenPosition : IEquatable<BetweenPosition>
{
    // The ID of the item before the new item (not the ID of the item that the new item should be before)
    public Guid? Before { get; set; }
    // The ID of the item after the new item (not the ID of the item that the new item should be after)
    public Guid? After { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BetweenPosition);
    }

    public bool Equals(BetweenPosition? other)
    {
        if (other is null)
            return false;

        return Before == other.Before && After == other.After;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Before, After);
    }

    public static bool operator ==(BetweenPosition left, BetweenPosition right)
    {
        return EqualityComparer<BetweenPosition>.Default.Equals(left, right);
    }

    public static bool operator !=(BetweenPosition left, BetweenPosition right)
    {
        return !(left == right);
    }
}
