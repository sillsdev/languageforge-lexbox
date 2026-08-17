using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

public abstract class CollectionDiffApi<T, TId> where TId : notnull
{
    public virtual async Task<(int Changes, T Added)> AddAndGet(T value)
    {
        var changes = await Add(value);
        return (changes, value);
    }
    // Can be implemented instead of AddAndGet for simpler DX
    public virtual Task<int> Add(T value)
    {
        throw new NotImplementedException();
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

public class ObjectWithIdCollectionReplaceDiffApi<T>(Func<T, T, Task<int>> ReplaceFunc) : ObjectWithIdCollectionDiffApi<T> where T : IObjectWithId
{
    public override Task<(int, T)> AddAndGet(T value)
    {
        throw new InvalidOperationException($"{nameof(AddAndGet)} should never be called");
    }

    public override Task<int> Remove(T value)
    {
        throw new InvalidOperationException($"{nameof(Remove)} should never be called");
    }

    public override async Task<int> Replace(T before, T after)
    {
        return await ReplaceFunc(before, after);
    }
}

public interface IOrderableCollectionDiffApi<T, TId> where T : IOrderableNoId where TId : notnull
{
    Task<int> Add(T value, BetweenPosition<T> between);
    /// <summary>Deletes unconditionally; the moved-vs-deleted decision is the walk's (DiffOrderable with a MoveContext), not the implementation's.</summary>
    Task<int> Remove(T value);
    /// <summary>Positions the item in this api's own collection; implementations re-parent it first when it currently lives under a different parent (like MoveSense).</summary>
    Task<int> Move(T value, BetweenPosition<T> between);
    Task<int> Replace(T before, T after);
    TId GetId(T value);
}

public static class DiffCollection
{
    public static async Task<(int Changes, ICollection<T> Added)> DiffAndGetAdded<T, TId>(
        IList<T> before,
        IList<T> after,
        CollectionDiffApi<T, TId> diffApi,
        DeferredDeletes? deferredDeletes = null) where TId : notnull
    {
        var changes = 0;
        var afterEntriesDict = after.ToDictionary(diffApi.GetId);
        List<T>? toRemove = null;

        foreach (var beforeEntry in before)
        {
            var id = diffApi.GetId(beforeEntry);
            if (afterEntriesDict.TryGetValue(id, out var afterEntry))
            {
                changes += await diffApi.Replace(beforeEntry, afterEntry);
                afterEntriesDict.Remove(id);
            }
            else
            {
                (toRemove ??= []).Add(beforeEntry);
            }
        }

        foreach (var (id, value) in afterEntriesDict)
        {
            var (addChanges, added) = await diffApi.AddAndGet(value);
            changes += addChanges;
            afterEntriesDict[id] = added;
        }

        // removes are done last (or deferred until after the whole walk) to prevent cascading deletes
        // to entities that are being moved (e.g. senses being moved from a deleted entry to a new or updated entry)
        if (toRemove is not null)
        {
            foreach (var beforeEntry in toRemove)
            {
                if (deferredDeletes is not null)
                    deferredDeletes.Defer(() => diffApi.Remove(beforeEntry));
                else
                    changes += await diffApi.Remove(beforeEntry);
            }
        }

        return (changes, afterEntriesDict.Values);
    }


    public static async Task<int> Diff<T, TId>(
        IList<T> before,
        IList<T> after,
        CollectionDiffApi<T, TId> diffApi) where TId : notnull
    {
        var (changes, _) = await DiffAndGetAdded(before, after, diffApi);
        return changes;
    }

    public static async Task<int> DiffOrderable<T, TId>(
        IList<T> before,
        IList<T> after,
        IOrderableCollectionDiffApi<T, TId> diffApi,
        MoveContext<T, TId>? moves = null) where T : IOrderableNoId where TId : notnull
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
                        var removedEntry = before[diff.Index];
                        if (moves is null)
                        {
                            changes += await diffApi.Remove(removedEntry);
                        }
                        else if (moves.IsActuallyADelete(diffApi.GetId(removedEntry)))
                        {
                            moves.DeferDelete(() => diffApi.Remove(removedEntry));
                        }
                        // else it still exists in the after state: it's moving to a different parent, not being deleted
                        break;
                    case PositionDiffKind.Add:
                        var addedEntry = after[diff.Index];
                        between = GetStableBetween(diff.Index, after, stableIds, diffApi.GetId);
                        if (moves is not null && moves.IsActuallyAMove(diffApi.GetId(addedEntry), out var movedFrom))
                        {
                            // not a create: it existed in the before state under a different parent, so move it here
                            changes += await diffApi.Move(addedEntry, between);
                            changes += await diffApi.Replace(movedFrom, addedEntry);
                        }
                        else
                        {
                            changes += await diffApi.Add(addedEntry, between);
                        }
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

    private static BetweenPosition<T> GetStableBetween<T, TId>(int i, IList<T> current, HashSet<TId> stable, Func<T, TId> getId) where TId : notnull
    {
        T? previous = default;
        T? next = default;
        for (var j = i - 1; j >= 0; j--)
        {
            if (stable.Contains(getId(current[j])))
            {
                previous = current[j];
                break;
            }
        }
        for (var j = i + 1; j < current.Count; j++)
        {
            if (stable.Contains(getId(current[j])))
            {
                next = current[j];
                break;
            }
        }
        return new BetweenPosition<T>(previous, next);
    }

    private static ImmutableSortedDictionary<int, PositionDiff>? DiffPositions<T, TId>(
        IList<T> before,
        IList<T> after,
        Func<T, TId> getId) where TId : notnull
    {
        // Map TIds to integers for JSON serialization (supports non-Guid ID types like tuples)
        var idToInt = new Dictionary<TId, int>();
        int idx = 0;
        foreach (var item in before)
        {
            var id = getId(item);
            if (!idToInt.ContainsKey(id)) idToInt[id] = idx++;
        }
        foreach (var item in after)
        {
            var id = getId(item);
            if (!idToInt.ContainsKey(id)) idToInt[id] = idx++;
        }

        var beforeJson = new JsonArray([.. before.Select(item => JsonValue.Create(idToInt[getId(item)]))]);
        var afterJson = new JsonArray([.. after.Select(item => JsonValue.Create(idToInt[getId(item)]))]);
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

/// <summary>
/// Context about what exists globally, in the whole before and after states, so a collection diff
/// can tell moves between parents apart from creates and deletes.
/// </summary>
public class MoveContext<T, TId>(
    IReadOnlyDictionary<TId, T> allBefore,
    IReadOnlyDictionary<TId, T> allAfter,
    DeferredDeletes deferredDeletes) where TId : notnull
{
    /// <summary>False when the id still exists in the after state: it's moving somewhere else.</summary>
    public bool IsActuallyADelete(TId id) => !allAfter.ContainsKey(id);

    /// <summary>
    /// True when the id already existed in the before state: the add is a move from a different parent.
    /// Only consults the in-memory before state; if moving children ever becomes a FieldWorks Lite
    /// client feature this would need an api-lookup fallback, but today that would just be wasteful.
    /// </summary>
    public bool IsActuallyAMove(TId id, [MaybeNullWhen(false)] out T movedFrom) => allBefore.TryGetValue(id, out movedFrom);

    public void DeferDelete(Func<Task<int>> delete) => deferredDeletes.Defer(delete);
}

/// <summary>
/// Genuine deletes queued during a diff walk and run after it, so a parent's cascading delete
/// can never destroy a child that was moved out (all moves have already executed by then).
/// DiffAndGetAdded's removes-last ordering isn't enough for that: DiffOrderable runs removes first,
/// and one entry's nested deletes would run before a later entry's diff gets to move the child out.
/// The walk's owner must call DeleteAll after the walk completes; when the walk throws, the queued
/// deletes never run (the sync is aborted and re-diffed next time anyway).
/// </summary>
public class DeferredDeletes
{
    private readonly List<Func<Task<int>>> _deletes = [];

    public void Defer(Func<Task<int>> delete) => _deletes.Add(delete);

    public async Task<int> DeleteAll()
    {
        var changes = 0;
        // by index so a delete that defers another delete extends the drain instead of throwing
        for (var i = 0; i < _deletes.Count; i++)
        {
            changes += await _deletes[i]();
        }
        _deletes.Clear();
        return changes;
    }
}

public record BetweenPosition<T>(T? Previous, T? Next)
{
    public async Task<BetweenPosition> MapAsync(Func<T, Task<Guid?>> map)
    {
        return new BetweenPosition(
            Previous is null ? null : await map(Previous),
            Next is null ? null : await map(Next));
    }
}
public record BetweenPosition(Guid? Previous, Guid? Next) : BetweenPosition<Guid?>(Previous, Next);
