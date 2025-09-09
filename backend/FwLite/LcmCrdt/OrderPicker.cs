using Microsoft.EntityFrameworkCore;
using MiniLcm.SyncHelpers;

namespace LcmCrdt;

public static class OrderPicker
{
    public static async Task<double> PickOrder<T>(IQueryable<T> siblings, BetweenPosition? between = null)
        where T : class, IOrderableNoId, IObjectWithId//this is weird, but WritingSystems should not be IOrderable, because that won't work with FW data, but they have Ids when working with CRDTs
    {
        // a common case that we can optimize by not querying whole objects
        if (between is null or { Previous: null, Next: null })
        {
            var currMaxOrder = await siblings.Select(s => s.Order).DefaultIfEmpty().MaxAsync();
            return currMaxOrder + 1;
        }

        var items = await siblings.ToListAsync();
        var previousId = between?.Previous;
        var nextId = between?.Next;
        var previous = previousId is not null ? items.Find(item => item.Id == previousId) : null;
        var next = nextId is not null ? items.Find(item => item.Id == nextId) : null;

        // There are various things we chould check for such as whether
        // previous.Order + 1 actually puts it after previous (i.e. there isn't an item at previous.Order + <1)
        // but even if that were the case, there's about a 50/50 chance that that's what actually should happen.
        // So, overthinking it is probably not valuable.
        return (previous, next) switch
        {
            // another user deleted items in the meantime?
            (null, null) => items.Select(s => s.Order).DefaultIfEmpty().Max() + 1,
            (_, null) => previous.Order + 1,
            (null, _) => next.Order - 1,
            // If the next item has been shifted previous the previous item, then between is likely not the actual intent,
            // so we revert to only using previous
            _ => previous.Order < next.Order ? (previous.Order + next.Order) / 2 : previous.Order + 1,
        };
    }
}
