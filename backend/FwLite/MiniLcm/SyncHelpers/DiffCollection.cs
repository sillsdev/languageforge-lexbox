using System.Collections.Immutable;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
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

public interface IOrderableCollectionDiffApi<T> where T : IOrderable
{
    Task<int> Add(T value, BetweenPosition between);
    Task<int> Remove(T value);
    Task<int> Move(T value, BetweenPosition between);
    Task<int> Replace(T before, T after);

    Guid GetId(T value)
    {
        return value.Id;
    }
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

        var beforeEntriesDict = before.ToDictionary(diffApi.GetId);

        var postAddUpdates = new List<(T created, T after)>(after.Count);
        foreach (var afterEntry in after)
        {
            if (beforeEntriesDict.Remove(diffApi.GetId(afterEntry), out var beforeEntry))
            {
                postAddUpdates.Add((beforeEntry, afterEntry)); // defer updating existing entry
            }
            else
            {
                var (change, created) = await diffApi.AddAndGet(afterEntry); // create new entry
                changes += change;
                postAddUpdates.Add((created, afterEntry)); // defer updating new entry
            }
        }

        foreach ((var createdItem, var afterItem) in postAddUpdates)
        {
            //todo this may do a lot more work than it needs to, eg sense will be created during add, but they will be checked again here when we know they didn't change
            changes += await diffApi.Replace(createdItem, afterItem);
        }

        foreach (var beforeEntry in beforeEntriesDict.Values)
        {
            changes += await diffApi.Remove(beforeEntry);
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
        IOrderableCollectionDiffApi<T> diffApi) where T : IOrderable
    {
        var changes = 0;

        var positionDiffs = DiffPositions(before, after, diffApi.GetId);
        if (positionDiffs is not null)
        {
            // The positive keys in positionDiffs are the indexes of added or moved items. I.e. they're the unstable ones.
            // Deleted items are given a negative index. So, they aren't picked up here. They also don't exist in the after list, so they're not relevant.
            var stableIds = after.Where((_, i) => !positionDiffs.ContainsKey(i))
                .Select(diffApi.GetId)
                .ToHashSet();

            foreach (var (_, diff) in positionDiffs)
            {
                switch (diff.Kind)
                {
                    case PositionDiffKind.Move:
                        var movedEntry = after[diff.Index];
                        var between = GetStableBetween(diff.Index, after, stableIds, diffApi.GetId);
                        changes += await diffApi.Move(movedEntry, between);
                        stableIds.Add(diffApi.GetId(movedEntry));
                        break;
                    case PositionDiffKind.Remove:
                        changes += await diffApi.Remove(before[diff.Index]);
                        break;
                    case PositionDiffKind.Add:
                        var addedEntry = after[diff.Index];
                        between = GetStableBetween(diff.Index, after, stableIds, diffApi.GetId);
                        changes += await diffApi.Add(addedEntry, between);
                        stableIds.Add(diffApi.GetId(addedEntry));
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported position diff kind {diff.Kind}");
                }
            }
        }

        var afterEntriesDict = after.ToDictionary(diffApi.GetId);
        foreach (var beforeEntry in before)
        {
            if (afterEntriesDict.TryGetValue(diffApi.GetId(beforeEntry), out var afterEntry))
            {
                changes += await diffApi.Replace(beforeEntry, afterEntry);
            }
        }

        return changes;
    }

    private static BetweenPosition GetStableBetween<T>(int i, IList<T> current, HashSet<Guid> stable, Func<T, Guid> GetId)
    {
        T? beforeEntity = default;
        T? afterEntity = default;
        for (var j = i - 1; j >= 0; j--)
        {
            if (stable.Contains(GetId(current[j])))
            {
                beforeEntity = current[j];
                break;
            }
        }
        for (var j = i + 1; j < current.Count; j++)
        {
            if (stable.Contains(GetId(current[j])))
            {
                afterEntity = current[j];
                break;
            }
        }
        return new BetweenPosition(
            beforeEntity is not null ? GetId(beforeEntity) : null,
            afterEntity is not null ? GetId(afterEntity) : null);
    }

    private static ImmutableSortedDictionary<int, PositionDiff>? DiffPositions<T>(
        IList<T> before,
        IList<T> after,
        Func<T, Guid> GetId)
    {
        var beforeJson = new JsonArray([.. before.Select(item => JsonValue.Create(GetId(item)))]);
        var afterJson = new JsonArray([.. after.Select(item => JsonValue.Create(GetId(item)))]);
        return JsonDiffPatcher.Diff(beforeJson, afterJson, DiffFormatter.Instance);
    }

#pragma warning disable IDE0072 // Add missing cases
    /// <summary>
    /// Formats Json array diffs into a list sorted by:
    /// - deletes first (with arbitrary negative indexes)
    /// - added and moved in order of new/current index (using current index)
    /// </summary>
    private class DiffFormatter : IJsonDiffDeltaFormatter<ImmutableSortedDictionary<int, PositionDiff>>
    {
        public static readonly DiffFormatter Instance = new();

        private DiffFormatter() { }

        public ImmutableSortedDictionary<int, PositionDiff>? Format(ref JsonDiffDelta delta, JsonNode? left)
        {
            if (delta.Kind == DeltaKind.None) return null;

            return delta.GetArrayChangeEnumerable().Select(change =>
            {
                return change.Diff.Kind switch
                {
                    DeltaKind.ArrayMove => new PositionDiff(change.Diff.GetNewIndex(), PositionDiffKind.Move),
                    DeltaKind.Added => new PositionDiff(change.Index, PositionDiffKind.Add),
                    DeltaKind.Deleted => new PositionDiff(change.Index, PositionDiffKind.Remove),
                    _ => throw new InvalidOperationException("Only array diff results are supported"),
                };
            }).ToImmutableSortedDictionary(diff => diff.SortIndex, diff => diff);
        }
    }
#pragma warning restore IDE0072 // Add missing cases
}

public enum PositionDiffKind { Add, Remove, Move }
public record PositionDiff(int Index, PositionDiffKind Kind)
{
    // Indexes for add and move operations represent final positions.
    // I.e. the order of the diffs doesn't really have meaning, but rather the caller is expected to make sure that's where the item ends up.
    // Also, final position indexes might not yet exist in the current list.

    // So, the easiest way to make sure the caller will be able to apply the diffs sequentially is to order them so that:
    // - Deletes happens first
    // - Adds and moves are then ordered by the new index (i.e. we work from front to back)
    public int SortIndex => Kind == PositionDiffKind.Remove ? -Index - 1 : Index;
}

public record BetweenPosition<T>(T? Previous, T? Next);
public record BetweenPosition(Guid? Previous, Guid? Next) : BetweenPosition<Guid?>(Previous, Next);
