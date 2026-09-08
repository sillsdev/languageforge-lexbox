using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;

namespace MiniLcm.SyncHelpers;

/// <summary>
/// The entire sync's before/after view of one child type, so <see cref="MoveAwareOrderableDiffApi{T,TId}"/>
/// can tell reparenting apart from a genuine create or delete.
/// </summary>
public class MoveDetection<T, TId>(IReadOnlyDictionary<TId, T> allBefore, IReadOnlyDictionary<TId, T> allAfter)
    where TId : notnull
{
    /// <summary>The id existed somewhere in the before state; <paramref name="before"/> is that version, to diff a move against.</summary>
    public bool ExistedBefore(TId id, [MaybeNullWhen(false)] out T before) => allBefore.TryGetValue(id, out before);

    /// <summary>The id still exists somewhere in the after state => it's not deleted</summary>
    public bool ExistsAfter(TId id) => allAfter.ContainsKey(id);
}

/// <summary>Detects moves between parents and translates them into reparenting operations.</summary>
public class MoveAwareOrderableDiffApi<T, TId>(OrderableCollectionDiffApi<T, TId> inner, MoveDetection<T, TId> moves)
    : ForwardingOrderableDiffApi<T, TId>(inner) where T : IOrderableNoId where TId : notnull
{
    public override async Task<int> Add(T value, BetweenPosition<T> between)
    {
        if (!moves.ExistedBefore(GetId(value), out var before)) return await Inner.Add(value, between);
        return await Inner.Reparent(value, between) + await Inner.Replace(before, value);
    }

    public override async Task<int> Remove(T value)
    {
        if (!moves.ExistsAfter(GetId(value))) return await Inner.Remove(value);
        return 0; // still exists under another parent: that parent's diff owns the move
    }
}

/// <summary>Queues deletes (e.g. so that children can be reparented before they are cascade-deleted)</summary>
public class DeferringDeletesOrderableDiffApi<T, TId>(OrderableCollectionDiffApi<T, TId> inner, DeferredDeletes deferred)
    : ForwardingOrderableDiffApi<T, TId>(inner) where T : IOrderableNoId where TId : notnull
{
    public override Task<int> Remove(T value) => deferred.Defer(() => Inner.Remove(value));
}

/// <summary>Unordered twin of <see cref="DeferringDeletesOrderableDiffApi{T,TId}"/> (for the root entry list).</summary>
public class DeferringDeletesCollectionDiffApi<T, TId>(CollectionDiffApi<T, TId> inner, DeferredDeletes deferred)
    : ForwardingCollectionDiffApi<T, TId>(inner) where TId : notnull
{
    public override Task<int> Remove(T value) => deferred.Defer(() => Inner.Remove(value));
}

/// <summary>
/// Detects children in an new entity that are NOT new, but rather being reparented.
/// Ensures those children are handled by move-aware sync code, rather than submitting
/// the whole tree to the MiniLcmApi (which could throw on the live/duplicate guid).
/// </summary>
public class MovedInChildrenDiffApi<T, TId>(
    OrderableCollectionDiffApi<T, TId> inner,
    Func<T, bool> hasDescendantMovingIn,
    Func<T, T> withoutChildren)
    : ForwardingOrderableDiffApi<T, TId>(inner) where T : IOrderableNoId where TId : notnull
{
    public override async Task<int> Add(T value, BetweenPosition<T> between)
    {
        if (!hasDescendantMovingIn(value)) return await Inner.Add(value, between);
        var childless = withoutChildren(value);
        return await Inner.Add(childless, between) + await Inner.Replace(childless, value);
    }
}

/// <summary>
/// Unordered twin of <see cref="MovedInChildrenDiffApi{T,TId}"/> (for the root entry list). Also needs
/// <paramref name="withChildrenOf"/> because AddAndGet's result is what later phases diff against.
/// </summary>
public class MovedInChildrenCollectionDiffApi<T, TId>(
    CollectionDiffApi<T, TId> inner,
    Func<T, bool> hasDescendantMovingIn,
    Func<T, T> withoutChildren,
    Func<T, T, T> withChildrenOf)
    : ForwardingCollectionDiffApi<T, TId>(inner) where TId : notnull
{
    public override async Task<int> Add(T value) => (await AddAndGet(value)).Changes;

    public override async Task<(int Changes, T Added)> AddAndGet(T value)
    {
        if (!hasDescendantMovingIn(value)) return await Inner.AddAndGet(value);
        var childless = withoutChildren(value);
        var (changes, created) = await Inner.AddAndGet(childless);
        changes += await Inner.Replace(childless, value);
        return (changes, withChildrenOf(created, value));
    }
}

public class DeferredDeletes
{
    private readonly List<Func<Task<int>>> _deletes = [];

    /// <summary>Queues the delete; returns 0 changes now (they're counted when the queue is drained).</summary>
    public Task<int> Defer(Func<Task<int>> delete)
    {
        _deletes.Add(delete);
        return Task.FromResult(0);
    }

    public async Task<int> DeleteAll()
    {
        var changes = 0;
        // by index so a delete that defers another delete extends the drain instead of throwing
        for (var i = 0; i < _deletes.Count; i++)
            changes += await _deletes[i]();
        _deletes.Clear();
        return changes;
    }
}

/// <summary>Implements and forwards everything, so decorators only need to override what they actually change.</summary>
public abstract class ForwardingOrderableDiffApi<T, TId>(OrderableCollectionDiffApi<T, TId> inner)
    : OrderableCollectionDiffApi<T, TId> where T : IOrderableNoId where TId : notnull
{
    protected readonly OrderableCollectionDiffApi<T, TId> Inner = inner;
    public override TId GetId(T value) => Inner.GetId(value);
    public override Task<int> Add(T value, BetweenPosition<T> between) => Inner.Add(value, between);
    public override Task<int> Remove(T value) => Inner.Remove(value);
    public override Task<int> Move(T value, BetweenPosition<T> between) => Inner.Move(value, between);
    public override Task<int> Reparent(T value, BetweenPosition<T> between) => Inner.Reparent(value, between);
    public override Task<int> Replace(T before, T after) => Inner.Replace(before, after);
}

/// <summary>Unordered twin of <see cref="ForwardingOrderableDiffApi{T,TId}"/>.</summary>
public abstract class ForwardingCollectionDiffApi<T, TId>(CollectionDiffApi<T, TId> inner)
    : CollectionDiffApi<T, TId> where TId : notnull
{
    protected readonly CollectionDiffApi<T, TId> Inner = inner;
    public override TId GetId(T value) => Inner.GetId(value);
    public override Task<(int Changes, T Added)> AddAndGet(T value) => Inner.AddAndGet(value);
    public override Task<int> Add(T value) => Inner.Add(value);
    public override Task<int> Remove(T value) => Inner.Remove(value);
    public override Task<int> Replace(T before, T after) => Inner.Replace(before, after);
}
