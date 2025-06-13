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
        foreach (var group in dbContext.ChangeTracker.Entries()
                     .Where(e => e is { State: EntityState.Added or EntityState.Modified, Entity: Entry or Sense })
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
            toUpdate.Add(await ForUpdate(group, dbContext));
        }
        if (toUpdate is []) return;
        await EntrySearchService.UpdateEntrySearchTable(toUpdate, (LcmCrdtDbContext)dbContext);
    }

    private async Task<Entry> ForUpdate(IGrouping<Guid, EntityEntry> group, DbContext dbContext)
    {
        var entities = group.ToArray();
        //scope created so the entry variables don't collide
        {
            if (entities is [{ State: EntityState.Added, Entity: Entry entry }])
            {
                return entry;
            }
        }

        var fullEntry = await dbContext.Set<Entry>()
            .Include(e => e.Senses)
            .FirstOrDefaultAsync(e => e.Id == group.Key);
        if (fullEntry is null)
        {
            //null when a new entry is added along with some senses
            fullEntry = new Entry() { Id = group.Key };
        }

        foreach (var entity in entities)
        {
            if (entity.Entity is Sense sense)
            {

                fullEntry.Senses = [..fullEntry.Senses.Where(s => s.Id != sense.Id), sense];
            }
            else if (entity.Entity is Entry entry)
            {
                fullEntry = fullEntry with { LexemeForm = entry.LexemeForm, CitationForm = entry.CitationForm };
            }
        }

        return fullEntry;
    }
}
