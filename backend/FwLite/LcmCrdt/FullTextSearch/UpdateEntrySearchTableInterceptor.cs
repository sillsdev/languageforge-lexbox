using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace LcmCrdt.FullTextSearch;

public class UpdateEntrySearchTableInterceptor : ISaveChangesInterceptor
{
    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        //probably not needed, but we'll do it just in case
        UpdateSearchTableOnSave(eventData.Context).Wait();
        return default;
    }

    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await UpdateSearchTableOnSave(eventData.Context);
        return default;
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
            .FirstAsync(e => e.Id == group.Key);
        foreach (var entity in entities)
        {
            if (entity.Entity is Sense sense)
            {
                fullEntry.Senses = fullEntry.Senses.Select(s => s.Id == sense.Id ? sense : s).ToList();
            }
            else if (entity.Entity is Entry entry)
            {
                fullEntry = fullEntry with { LexemeForm = entry.LexemeForm, CitationForm = entry.CitationForm };
            }
        }

        return fullEntry;
    }
}
