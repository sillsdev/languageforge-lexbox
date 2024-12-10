using Microsoft.EntityFrameworkCore;
using MiniLcm.SyncHelpers;

namespace LcmCrdt;

public static class OrderPicker
{
    public static async Task<double> PickOrder<T>(IQueryable<T> siblings, BetweenPosition? between = null) where T : class, IOrderable
    {
        // a common case that we can optimize by not querying whole objects
        if (between is null or { Before: null, After: null })
        {
            var currMaxOrder = siblings.Select(s => s.Order).DefaultIfEmpty().Max();
            return currMaxOrder + 1;
        }

        var items = await siblings.ToListAsync();
        var beforeId = between?.Before;
        var afterId = between?.After;
        var before = beforeId is not null ? items.Find(item => item.Id == beforeId) : null;
        var after = afterId is not null ? items.Find(item => item.Id == afterId) : null;

        // There are various things we chould check for such as whether
        // before.Order + 1 actually puts it after before (i.e. there isn't an item at before.Order + <1)
        // but even if that were the case, there's about a 50/50 chance that that's what actually should happen.
        // So, overthinking it is probably not valuable.
        return (before, after) switch
        {
            // another user deleted items in the meantime?
            (null, null) => siblings.Select(s => s.Order).DefaultIfEmpty().Max() + 1,
            (_, null) => before.Order + 1,
            (null, _) => after.Order - 1,
            // If the after item has been shifted before the before item, then between is likely not the actual intent,
            // so we revert to only using before
            _ => before.Order < after.Order ? (before.Order + after.Order) / 2 : before.Order + 1,
        };
    }
}
