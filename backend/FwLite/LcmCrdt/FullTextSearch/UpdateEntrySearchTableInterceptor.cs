using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace LcmCrdt.FullTextSearch;

public class UpdateEntrySearchTableInterceptor : ISaveChangesInterceptor
{
    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        throw new NotImplementedException(
            "SavingChanges sync is not implemented. Use SavingChangesAsync instead.");
        // If this needs to be supported, try this, but it may cause deadlocks or other issues.
        // Alternatively, you can, check to see if there are any entry changes and throw only if there are.
        // UpdateSearchTableOnSave(eventData.Context).Wait();
        // return default;
    }

    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await UpdateSearchTableOnSave(eventData.Context);
        return result;
    }

    private async Task UpdateSearchTableOnSave(DbContext? dbContext)
    {
        if (dbContext is null) return;
        List<Entry> toUpdate = [];
        List<Guid> toRemove = [];
        var newWritingSystems = dbContext.ChangeTracker.Entries()
            .Where(e => e.Entity is WritingSystem && e.State == EntityState.Added)
            .Select(e => (WritingSystem)e.Entity).ToList();
        foreach (var group in dbContext.ChangeTracker.Entries()
                     .Where(e => e is { State: EntityState.Added or EntityState.Modified or EntityState.Deleted, Entity: Entry or Sense })
                     .GroupBy(e =>
                     {
                         return e.Entity switch
                         {
                             Entry entry => entry.Id,
                             Sense sense => sense.EntryId,
                             _ => throw new InvalidOperationException(
                                 $"Entity is not Entry or Sense: {e.Entity.GetType().Name}")
                         };
                     }))
        {
            var (updatedEntry, removed) = await ForUpdate(group, group.Key, dbContext);
            if (updatedEntry is not null) toUpdate.Add(updatedEntry);
            if (removed is not null) toRemove.Add(removed.Value);
        }
        if (toUpdate is [] && toRemove is []) return;
        await EntrySearchService.UpdateEntrySearchTable(toUpdate, toRemove, newWritingSystems, (LcmCrdtDbContext)dbContext);
    }

    private async Task<(Entry? updatedEntry, Guid? removed)> ForUpdate(IEnumerable<EntityEntry> group, Guid entryId, DbContext dbContext)
    {
        var entities = group.ToArray();
        //scope created so the entry variables don't collide
        {
            if (entities is [{ State: EntityState.Added, Entity: Entry entry }])
            {
                return (entry, null);
            }
        }

        Entry? fullEntry = null;
        var sensesToReplace = new List<Sense>();
        var sensesToRemove = new List<Guid>();

        foreach (var entity in entities)
        {
            if (entity.Entity is Sense sense)
            {
                if (entity.State == EntityState.Deleted)
                {
                    sensesToRemove.Add(sense.Id);
                    continue;
                }
                sensesToReplace.Add(sense);
            }
            else if (entity.Entity is Entry entry)
            {
                if (entity.State == EntityState.Deleted)
                {
                    return (null, entry.Id);
                }
                if (entity.State == EntityState.Added)
                {
                    fullEntry = entry;
                    continue;
                }

                fullEntry = await dbContext.Set<Entry>()
                    .Include(e => e.Senses)
                    .FirstAsync(e => e.Id == entryId);
                fullEntry.LexemeForm = entry.LexemeForm;
                fullEntry.CitationForm = entry.CitationForm;
            }
        }

        fullEntry ??= await dbContext.Set<Entry>()
            .Include(e => e.Senses)
            .FirstAsync(e => e.Id == entryId);
        fullEntry.Senses = [..fullEntry.Senses.Where(s => !sensesToRemove.Contains(s.Id) && sensesToReplace.All(sr => sr.Id != s.Id)), ..sensesToReplace];

        return (fullEntry, null);
    }
}
