using Humanizer;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using System.Text.RegularExpressions;
using MiniLcm.Exceptions;

namespace LcmCrdt;

public record ProjectActivity(
    Guid CommitId,
    DateTimeOffset Timestamp,
    List<ChangeEntity<IChange>> Changes,
    CommitMetadata Metadata)
{
    public string ChangeName => HistoryService.ChangesNameHelper(Changes);
}

public record ChangeContext(
    Guid CommitId,
    int ChangeIndex,
    string ChangeName,
    IObjectWithId Snapshot,
    ICollection<Entry> AffectedEntries)
{
    public ChangeContext(ChangeEntity<IChange> change, IObjectWithId snapshot, ICollection<Entry> affectedEntries)
        : this(change.CommitId, change.Index, HistoryService.ChangeNameHelper(change.Change), snapshot, affectedEntries)
    {
    }
    public string EntityType => Snapshot?.GetType().Name ?? "Unknown";
}

public record HistoryLineItem(
    Guid CommitId,
    CommitMetadata Metadata,
    Guid EntityId,
    DateTimeOffset Timestamp,
    Guid? SnapshotId,
    int changeIndex,
    ChangeEntity<IChange> change,
    string ChangeName,
    IObjectWithId? Entity)
{
    public HistoryLineItem(
        Commit commit,
        Guid entityId,
        DateTimeOffset timestamp,
        Guid? snapshotId,
        int changeIndex,
        ChangeEntity<IChange> change,
        IObjectBase? entity) : this(commit.Id,
        commit.Metadata,
        entityId,
        timestamp,
        snapshotId,
        changeIndex,
        change,
        HistoryService.ChangeNameHelper(change.Change),
        (IObjectWithId?)entity?.DbObject)
    {
    }
}

public class HistoryService(DataModel dataModel, Microsoft.EntityFrameworkCore.IDbContextFactory<LcmCrdtDbContext> dbContextFactory, IMiniLcmApi miniLcmApi)
{
    public async IAsyncEnumerable<ProjectActivity> ProjectActivity(int skip = 0, int take = 100)
    {
        await using ICrdtDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var changeEntities = dbContext.Set<ChangeEntity<IChange>>();
        var query =
            from commit in dbContext.Commits.DefaultOrderDescending()
            join changeEntity in changeEntities
                on commit.Id equals changeEntity.CommitId into changes
            join snapshot in dbContext.Snapshots
                on commit.Id equals snapshot.CommitId into snapshots
            select new ProjectActivity(commit.Id,
                NormalizeTimestamp(commit.HybridDateTime.DateTime),
                changes.ToList(),
                commit.Metadata);
        await foreach (var projectActivity in query.Skip(skip).Take(take).ToLinqToDB().AsAsyncEnumerable())
        {
            yield return projectActivity;
        }
    }

    public async Task<ObjectSnapshot?> GetSnapshot(Guid snapshotId)
    {
        await using ICrdtDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Snapshots.SingleOrDefaultAsync(s => s.Id == snapshotId);
    }

    public async Task<IObjectWithId> GetObject(Guid commitId, Guid entityId)
    {
        return await dataModel.GetAtCommit<IObjectWithId>(commitId, entityId);
    }

    public async Task<IObjectWithId> GetObject(DateTime timestamp, Guid entityId)
    {
        //todo requires the timestamp to be exact, otherwise the change made on that timestamp will not be included
        //consider using a commitId and looking up the timestamp, but then we should be exact to the commit which we aren't right now.
        return await dataModel.GetAtTime<IObjectWithId>(new DateTimeOffset(timestamp), entityId);
    }

    public async IAsyncEnumerable<HistoryLineItem> GetHistory(Guid entityId)
    {
        await using ICrdtDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var changeEntities = dbContext.Set<ChangeEntity<IChange>>();
        var query =
            from commit in dbContext.Commits.DefaultOrder()
            from snapshot in dbContext.Snapshots.LeftJoin(
                s => s.CommitId == commit.Id && s.EntityId == entityId)
            from change in changeEntities.LeftJoin(c =>
                c.CommitId == commit.Id && c.EntityId == entityId)
#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
            where snapshot.Id != null || change.EntityId != null
#pragma warning restore CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
            select new HistoryLineItem(commit,
                entityId,
                NormalizeTimestamp(commit.HybridDateTime.DateTime),
                snapshot.Id,
                change.Index,
                change,
                snapshot.Entity);
        await foreach (var historyLineItem in query.ToLinqToDB().AsAsyncEnumerable())
        {
            yield return historyLineItem;
        }
    }

    public async Task<ChangeContext> LoadChangeContext(Guid commitId, int changeIndex)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        ICrdtDbContext crdtDbContext = dbContext;
        var change = await crdtDbContext.Commits
            .Where(c => c.Id == commitId)
            .SelectMany(c => c.ChangeEntities)
            .Where(ce => ce.Index == changeIndex)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"Change {changeIndex} not found in commit {commitId}");

        var snapshot = await dataModel.GetAtCommit<IObjectWithId>(commitId, change.EntityId);

        var affectedEntries = await GetAffectedEntryIds(change)
            .SelectAwait(async entryId => await GetCurrentOrLatestEntry(entryId))
            .ToArrayAsync();

        return new ChangeContext(change, snapshot, affectedEntries);
    }

    private async Task<Entry> GetCurrentOrLatestEntry(Guid entryId)
    {
        var entry = await miniLcmApi.GetEntry(entryId);
        if (entry is not null)
        {
            return entry;
        }

        // it was presumably deleted, so we'll fall back to the latest snapshot
        // (which does not include senses or any other references)
        return await dataModel.GetLatest<Entry>(entryId)
            ?? throw NotFoundException.ForType<Entry>(entryId);
    }

    private async IAsyncEnumerable<Guid> GetAffectedEntryIds(ChangeEntity<IChange> changeEntity)
    {
        if (changeEntity.Change.EntityType == typeof(Entry))
        {
            yield return changeEntity.EntityId;
        }
        else if (changeEntity.Change.EntityType == typeof(Sense))
        {
            var sense = await dataModel.GetLatest<Sense>(changeEntity.EntityId)
                ?? throw NotFoundException.ForType<Sense>(changeEntity.EntityId);
            yield return sense.EntryId;
        }
        else if (changeEntity.Change.EntityType == typeof(ExampleSentence))
        {
            var example = await dataModel.GetLatest<ExampleSentence>(changeEntity.EntityId)
                ?? throw NotFoundException.ForType<ExampleSentence>(changeEntity.EntityId);
            var sense = await dataModel.GetLatest<Sense>(example.SenseId)
                ?? throw NotFoundException.ForType<Sense>(example.SenseId);
            yield return sense.EntryId;
        }
        else if (changeEntity.Change.EntityType == typeof(ComplexFormComponent))
        {
            var cfc = await dataModel.GetLatest<ComplexFormComponent>(changeEntity.EntityId)
                ?? throw NotFoundException.ForType<ComplexFormComponent>(changeEntity.EntityId);
            yield return cfc.ComplexFormEntryId;
            yield return cfc.ComponentEntryId;
        }
    }

    internal static DateTimeOffset NormalizeTimestamp(DateTimeOffset timestamp)
    {
        // Linq2DB materializes datetime columns as local time; reinterpret the captured ticks as UTC to avoid DST offsets.
        // see: https://github.com/sillsdev/languageforge-lexbox/issues/2092
        return new DateTimeOffset(timestamp.Ticks, TimeSpan.Zero);
    }

    public static string ChangesNameHelper(List<ChangeEntity<IChange>> changeEntities)
    {
        return changeEntities switch
        {
            { Count: 0 } => "No changes",
            { Count: 1 } => ChangeNameHelper(changeEntities[0].Change),
            { Count: > 10 } => $"{changeEntities.Count} changes",
            { Count: var count } => $"{ChangeNameHelper(changeEntities[0].Change)} (+{count - 1} other change{(count > 2 ? "s" : "")})",
        };
    }

    public static string ChangeNameHelper(IChange change)
    {
        var type = change.GetType();
        //todo call JsonPatchChange.Summarize() instead of this
        if (type.Name.Contains("JsonPatch")) return $"Edit{change.EntityType.Name}".Humanize();
        else if (type.Name.StartsWith("DeleteChange`")) return $"Delete{change.EntityType.Name}".Humanize();
        else if (type.Name.StartsWith("SetOrderChange`")) return $"Reorder{change.EntityType.Name}".Humanize();
        var changeName = type.Name.Humanize();
        return Regex.Replace(changeName, " Change$", "", RegexOptions.IgnoreCase);
    }
}
