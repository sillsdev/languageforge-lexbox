using Humanizer;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using LinqToDB;
using SIL.Harmony.Entities;

namespace LcmCrdt;
public record ProjectActivity(
    Guid CommitId,
    DateTimeOffset Timestamp,
    List<ChangeEntity<IChange>> Changes)
{
    public string ChangeName => ChangeNameHelper(Changes);

    private static string ChangeNameHelper(List<ChangeEntity<IChange>> changeEntities)
    {
        return changeEntities switch
        {
            { Count: 0 } => "No changes",
            { Count: 1 } => changeEntities[0].Change switch
            {
                //todo call JsonPatchChange.Summarize() instead of this
                IChange change when change.GetType().Name.StartsWith("JsonPatchChange") => "Change " +
                    change.EntityType.Name,
                IChange change => change.GetType().Name.Humanize()
            },
            { Count: var count } => $"{count} changes"
        };
    }
}

public record HistoryLineItem(
    Guid CommitId,
    Guid EntityId,
    DateTimeOffset Timestamp,
    Guid? SnapshotId,
    string? ChangeName,
    IObjectWithId? Entity,
    string? EntityName)
{
    public HistoryLineItem(
        Guid commitId,
        Guid entityId,
        DateTimeOffset timestamp,
        Guid? snapshotId,
        IChange? change,
        IObjectBase? entity,
        string typeName) : this(commitId,
        entityId,
        new DateTimeOffset(timestamp.Ticks,
            TimeSpan.Zero), //todo this is a workaround for linq2db bug where it reads a date and assumes it's local when it's UTC
        snapshotId,
        change?.GetType().Name,
        (IObjectWithId?) entity?.DbObject,
        typeName)
    {
    }
}

public class HistoryService(ICrdtDbContext dbContext, DataModel dataModel)
{
    public IAsyncEnumerable<ProjectActivity> ProjectActivity()
    {
        return dbContext.Commits
            .DefaultOrderDescending()
            .Take(20)
            .Select(c => new ProjectActivity(c.Id, c.HybridDateTime.DateTime, c.ChangeEntities))
            .AsAsyncEnumerable();
    }

    public async Task<ObjectSnapshot?> GetSnapshot(Guid snapshotId)
    {
        return await dbContext.Snapshots.SingleOrDefaultAsync(s => s.Id == snapshotId);
    }

    public async Task<IObjectWithId> GetObject(DateTime timestamp, Guid entityId)
    {
        //todo requires the timestamp to be exact, otherwise the change made on that timestamp will not be included
        //consider using a commitId and looking up the timestamp, but then we should be exact to the commit which we aren't right now.
        return await dataModel.GetAtTime<IObjectWithId>(new DateTimeOffset(timestamp), entityId);
    }

    public IAsyncEnumerable<HistoryLineItem> GetHistory(Guid entityId)
    {
        var query = from commit in dbContext.Commits.DefaultOrder()
            from snapshot in dbContext.Snapshots.LeftJoin(
                s => s.CommitId == commit.Id && s.EntityId == entityId)
            from change in dbContext.Set<ChangeEntity<IChange>>().LeftJoin(c =>
                c.CommitId == commit.Id && c.EntityId == entityId)
            where snapshot.Id != null || change.EntityId != null
            select new HistoryLineItem(commit.Id,
                entityId,
                commit.HybridDateTime.DateTime,
                snapshot.Id,
                change.Change,
                snapshot.Entity,
                snapshot.TypeName);
        return query.AsAsyncEnumerable();
    }
}
