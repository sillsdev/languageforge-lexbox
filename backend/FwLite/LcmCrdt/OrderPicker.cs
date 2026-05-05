using Microsoft.EntityFrameworkCore;
using MiniLcm.SyncHelpers;

namespace LcmCrdt;

public static class OrderPicker
{
    /// <summary>
    /// Per-sibling-scope state threaded through <see cref="PickOrder{T}"/> when a bulk-change
    /// batch is active. The batch's writes aren't visible to the <c>siblings</c> DB query yet,
    /// so we track the orders they would have had here:
    /// <list type="bullet">
    ///   <item><see cref="EntityOrders"/> — orders issued to specific entities in this batch
    ///     (creates and moves). Looked up when the caller names a <c>between</c> anchor so
    ///     that a just-queued move/create of that anchor is respected instead of its stale
    ///     DB row (or its absence from the DB).</item>
    ///   <item><see cref="MaxIssuedOrder"/> — monotonically-increasing upper bound on orders
    ///     issued in this batch. Feeds the unconstrained-append case so we never return an
    ///     order below something we already issued.</item>
    ///   <item><see cref="CachedDbMaxOrder"/> — DB-side max for this scope, queried once per
    ///     batch on the first unconstrained append. Combined with <see cref="MaxIssuedOrder"/>
    ///     to produce the effective scope max — this is the fix for the old HWM being
    ///     set to (e.g.) a midpoint value of 1.5 while the DB still has a sibling at 3.</item>
    /// </list>
    /// </summary>
    public sealed class BatchOrderScope
    {
        public Dictionary<Guid, double> EntityOrders { get; } = [];
        public double MaxIssuedOrder { get; set; } = double.NegativeInfinity;
        public double? CachedDbMaxOrder { get; set; }
    }

    /// <summary>
    /// Picks an order value for a new or moved sibling.
    /// </summary>
    /// <param name="siblings">DB-visible siblings of the scope being ordered.</param>
    /// <param name="between">Optional anchors identifying where the new sibling should land.</param>
    /// <param name="batch">
    /// Batch state when called under a bulk-change scope. When present, the anchors in
    /// <paramref name="between"/> are resolved via <see cref="BatchOrderScope.EntityOrders"/>
    /// first (so in-flight moves/creates of the anchor are respected), falling back to the DB
    /// row; and the unconstrained-append branch uses <c>max(DB max, batch max) + 1</c> so a
    /// later append never lands below an earlier-issued order or an existing DB sibling.
    /// </param>
    /// <remarks>
    /// The generic constraint set is a bit odd: WritingSystems shouldn't be <c>IOrderable</c>
    /// (they aren't orderable in FwData), but they have Ids when working with CRDTs.
    /// </remarks>
    public static async Task<double> PickOrder<T>(
        IQueryable<T> siblings,
        BetweenPosition? between = null,
        BatchOrderScope? batch = null)
        where T : class, IOrderableNoId, IObjectWithId
    {
        // Unconstrained append: one past the true max of this scope, DB and batch combined.
        if (between is null or { Previous: null, Next: null })
        {
            var dbMax = batch?.CachedDbMaxOrder
                ?? await siblings.Select(s => s.Order).DefaultIfEmpty().MaxAsync();
            if (batch is not null) batch.CachedDbMaxOrder = dbMax;
            var effectiveMax = batch is not null ? Math.Max(dbMax, batch.MaxIssuedOrder) : dbMax;
            return effectiveMax + 1;
        }

        var items = await siblings.ToListAsync();
        var previousId = between.Previous;
        var nextId = between.Next;
        var previousDb = previousId is not null ? items.Find(item => item.Id == previousId) : null;
        var nextDb = nextId is not null ? items.Find(item => item.Id == nextId) : null;

        // Resolve each anchor's order preferring the batch's issued value over the DB row:
        // a queued move of the anchor already changed its order in the caller's mental model.
        // If the anchor isn't in the batch, fall back to whatever the DB row said.
        double? previousOrder = ResolveAnchorOrder(previousId, previousDb?.Order, batch);
        double? nextOrder = ResolveAnchorOrder(nextId, nextDb?.Order, batch);

        // Same branching as before — just operating on resolved orders instead of raw DB rows.
        // Choosing to not fight simultaneous inserts at the exact same position is intentional:
        // the second insert in such a race gets previous + 1 and the caller's downstream
        // reconciliation sorts it out.
        return (previousOrder, nextOrder) switch
        {
            // Caller named anchors but neither resolves — fall back to a pure append.
            (null, null) => items.Select(s => s.Order).DefaultIfEmpty().Max() + 1,
            (_, null) => previousOrder.Value + 1,
            (null, _) => nextOrder.Value - 1,
            // If the two anchors are inverted (possible if the caller is out of date),
            // revert to appending past previous rather than producing a negative span.
            _ => previousOrder.Value < nextOrder.Value
                ? (previousOrder.Value + nextOrder.Value) / 2
                : previousOrder.Value + 1,
        };
    }

    private static double? ResolveAnchorOrder(Guid? id, double? dbOrder, BatchOrderScope? batch)
    {
        if (id is null) return null;
        if (batch is not null && batch.EntityOrders.TryGetValue(id.Value, out var batched))
            return batched;
        return dbOrder;
    }
}
