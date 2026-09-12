using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using SIL.Harmony.Db;

namespace LcmCrdt.FullTextSearch;

// Keeps the FTS entry-search table in sync with entry/sense/morph-type changes.
//
// Two entry points are needed because there are two ways the projected tables are written:
//   * IProjectedEntityInterceptor — Harmony projects CRDT entities to the SQL tables with raw SQL
//     (bypassing EF change tracking), so this is the path exercised by the normal API (AddChange).
//   * ISaveChangesInterceptor — direct EF writes to the projected tables (e.g. tests, and any code
//     that edits the projected DbSets and calls SaveChanges) still go through EF change tracking.
// A given write goes through exactly one of these paths, so there is no double processing.
public class UpdateEntrySearchTableInterceptor : ISaveChangesInterceptor, IProjectedEntityInterceptor
{
    public async ValueTask OnProjectedEntitiesChanged(ProjectedEntityBatch batch)
    {
        if (batch.DbContext is not LcmCrdtDbContext dbContext) return;

        // A morph type's prefix/postfix tokens feed into every entry's headword, so any morph-type
        // change invalidates the whole search table. Morph types are seeded and only ever modified
        // (never added or deleted) in normal use, so this is rare.
        var morphTypeChanged = batch.Changes.Any(c => c.ClrType == typeof(MorphType));
        if (morphTypeChanged)
        {
            await EntrySearchService.RegenerateEntrySearchTable(dbContext);
            return;
        }

        var entryIdsToRemove = new List<Guid>();
        var entryIdsToUpdate = new HashSet<Guid>();
        foreach (var change in batch.Changes)
        {
            if (change.ClrType == typeof(Entry))
            {
                if (change.Kind == ProjectedChangeKind.Delete) entryIdsToRemove.Add(change.EntityId);
                else entryIdsToUpdate.Add(change.EntityId);
            }
            else if (change.Entity is Sense sense)
            {
                // A sense change (including deletion) updates its parent entry's search record.
                entryIdsToUpdate.Add(sense.EntryId);
            }
        }
        // A deleted entry is removed, not re-indexed, even if one of its senses also changed.
        entryIdsToUpdate.ExceptWith(entryIdsToRemove);

        if (entryIdsToUpdate.Count == 0 && entryIdsToRemove.Count == 0) return;

        Entry[] toUpdate = entryIdsToUpdate.Count == 0
            ? []
            : await dbContext.Set<Entry>()
                .Include(e => e.Senses)
                .Where(e => entryIdsToUpdate.Contains(e.Id))
                .ToArrayAsync();
        // The projection SQL has already run, so dbContext.WritingSystems reflects any writing systems
        // added in this batch; no separate newWritingSystems list is needed (unlike the EF path below,
        // which runs before SaveChanges commits).
        await EntrySearchService.UpdateEntrySearchTable(toUpdate, entryIdsToRemove, [], dbContext);
    }

    private bool EntryTableNeedsRegeneration { get; set; } = false;
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

    public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (EntryTableNeedsRegeneration) await RegenerateSearchTableAfterSave(eventData.Context);
        return result;
    }

    private async Task UpdateSearchTableOnSave(DbContext? maybeDbContext)
    {
        if (maybeDbContext is not LcmCrdtDbContext dbContext) return;
        // Morph types with changes to prefix or postfix tokens will require updated entry search records
        // (Note that morph types can't be added or deleted, so we only need to catch changes, which will be rare)
        var changedMorphTypes = dbContext.ChangeTracker.Entries<MorphType>()
            .Any(e => e.State == EntityState.Modified && (e.Property(m => m.Prefix).IsModified || e.Property(m => m.Postfix).IsModified));
        if (changedMorphTypes)
        {
            // The actual table regeneration will happen in the SavedChangesAsync handler; here we just flag that it will be needed
            EntryTableNeedsRegeneration = true;
            // Any change to morph-type tokens will invalidate the whole entry search table, so no need to check for individual entries
            return;
        }
        List<Entry> toUpdate = [];
        List<Guid> toRemove = [];
        var newWritingSystems = dbContext.ChangeTracker.Entries<WritingSystem>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity).ToList();
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
        await EntrySearchService.UpdateEntrySearchTable(toUpdate, toRemove, newWritingSystems, dbContext);
    }

    private async Task RegenerateSearchTableAfterSave(DbContext? maybeDbContext)
    {
        EntryTableNeedsRegeneration = false;
        if (maybeDbContext is not LcmCrdtDbContext dbContext) return;
        await EntrySearchService.RegenerateEntrySearchTable(dbContext);
    }

    // If saving changes fails, then the MorphType changes didn't make it into the DB and headwords don't need to be regenerated after all
    public void SaveChangesFailed(DbContextErrorEventData eventData) => EntryTableNeedsRegeneration = false;

    public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        EntryTableNeedsRegeneration = false;
        return Task.CompletedTask;
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
