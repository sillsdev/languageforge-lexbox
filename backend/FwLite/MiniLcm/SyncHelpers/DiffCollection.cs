using System.Text.Json.JsonDiffPatch;
using System.Text.Json.Nodes;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

public abstract class CollectionDiffApi<T, TId> where TId : notnull
{
    public abstract Task<int> Add(T value);
    public virtual async Task<(int, T)> AddAndGet(T value)
    {
        var changes = await Add(value);
        return (changes, value);
    }
    public abstract Task<int> Remove(T value);
    public abstract Task<int> Replace(T before, T after);
    public abstract TId GetId(T value);
}

public abstract class ObjectWithIdCollectionDiffApi<T> : CollectionDiffApi<T, Guid> where T : IObjectWithId
{
    public override Guid GetId(T value)
    {
        return value.Id;
    }
}

public interface OrderableCollectionDiffApi<T> where T : IOrderable
{
    Task<int> Add(T value, BetweenPosition between);
    Task<int> Remove(T value);
    Task<int> Move(T value, BetweenPosition between);
    Task<int> Replace(T before, T after);
}

public static class DiffCollection
{
    /// <summary>
    /// Diffs a list, for new items calls add, it will then call update for the item returned from the add, using that as the before item for the replace call
    /// </summary>
    public static async Task<int> DiffAddThenUpdate<T, TId>(
        IList<T> before,
        IList<T> after,
        CollectionDiffApi<T, TId> diffApi) where TId : notnull
    {
        var changes = 0;
        var afterEntriesDict = after.ToDictionary(diffApi.GetId);

        foreach (var beforeEntry in before)
        {
            if (afterEntriesDict.TryGetValue(diffApi.GetId(beforeEntry), out var afterEntry))
            {
                changes += await diffApi.Replace(beforeEntry, afterEntry);
            }
            else
            {
                changes += await diffApi.Remove(beforeEntry);
            }

            afterEntriesDict.Remove(diffApi.GetId(beforeEntry));
        }

        var postAddUpdates = new List<(T created, T after)>(afterEntriesDict.Values.Count);
        foreach (var value in afterEntriesDict.Values)
        {
            var (change, created) = await diffApi.AddAndGet(value);
            changes += change;
            postAddUpdates.Add((created, value));
        }
        foreach ((var createdItem, var afterItem) in postAddUpdates)
        {
            //todo this may do a lot more work than it needs to, eg sense will be created during add, but they will be checked again here when we know they didn't change
            await diffApi.Replace(createdItem, afterItem);
        }

        return changes;
    }

    public static async Task<int> Diff<T, TId>(
        IList<T> before,
        IList<T> after,
        CollectionDiffApi<T, TId> diffApi) where TId : notnull
    {
        var changes = 0;
        var afterEntriesDict = after.ToDictionary(diffApi.GetId);
        foreach (var beforeEntry in before)
        {
            if (afterEntriesDict.TryGetValue(diffApi.GetId(beforeEntry), out var afterEntry))
            {
                changes += await diffApi.Replace(beforeEntry, afterEntry);
            }
            else
            {
                changes += await diffApi.Remove(beforeEntry);
            }

            afterEntriesDict.Remove(diffApi.GetId(beforeEntry));
        }

        foreach (var value in afterEntriesDict.Values)
        {
            changes += await diffApi.Add(value);
        }

        return changes;
    }

    public static async Task<int> DiffOrderable<T>(
        IList<T> before,
        IList<T> after,
        OrderableCollectionDiffApi<T> diffApi) where T : IOrderable
    {
        var positionDiffs = DiffPositions(before, after)
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
                var between = GetStableBetween(diff.To.Value, after, stableIds);
                changes += await diffApi.Move(afterEntry, between);
                stableIds.Add(afterEntry.Id);
            }
            else if (diff.From is not null)
            {
                changes += await diffApi.Remove(before[diff.From.Value]);
            }
            else if (diff.To is not null)
            {
                var afterEntry = after[diff.To.Value];
                var between = GetStableBetween(diff.To.Value, after, stableIds);
                changes += await diffApi.Add(afterEntry, between);
                stableIds.Add(afterEntry.Id);
            }
        }

        var afterEntriesDict = after.ToDictionary(entry => entry.Id);
        foreach (var beforeEntry in before)
        {
            if (afterEntriesDict.TryGetValue(beforeEntry.Id, out var afterEntry))
            {
                changes += await diffApi.Replace(beforeEntry, afterEntry);
            }
        }

        return changes;
    }

    private static BetweenPosition GetStableBetween<T>(int i, IList<T> current, IReadOnlyList<Guid> stable) where T : IOrderable
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
            Previous = beforeEntity?.Id,
            Next = afterEntity?.Id
        };
    }

    private static IEnumerable<PositionDiff> DiffPositions<T>(
        IList<T> before,
        IList<T> after) where T : IOrderable
    {
        var beforeJson = new JsonArray(before.Select(item => JsonValue.Create(item.Id)).ToArray());
        var afterJson = new JsonArray(after.Select(item => JsonValue.Create(item.Id)).ToArray());

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
    public Guid? Previous { get; set; }
    public Guid? Next { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BetweenPosition);
    }

    public bool Equals(BetweenPosition? other)
    {
        if (other is null)
            return false;

        return Previous == other.Previous && Next == other.Next;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Previous, Next);
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
