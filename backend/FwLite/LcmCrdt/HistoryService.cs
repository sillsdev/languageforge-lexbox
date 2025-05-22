using System.Diagnostics.CodeAnalysis;
using Humanizer;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SIL.Harmony.Entities;
using System.Text.RegularExpressions;

namespace LcmCrdt;
public record ProjectActivity(
    Guid CommitId,
    DateTimeOffset Timestamp,
    List<ChangeEntity<IChange>> Changes,
    CommitMetadata Metadata)
{
    public string ChangeName => HistoryService.ChangesNameHelper(Changes);
}

public record HistoryLineItem(
    Guid CommitId,
    Guid EntityId,
    DateTimeOffset Timestamp,
    Guid? SnapshotId,
    int changeIndex,
    string? ChangeName,
    IObjectWithId? Entity,
    string? EntityName,
    string? AuthorName)
{
    public HistoryLineItem(
        Guid commitId,
        Guid entityId,
        DateTimeOffset timestamp,
        Guid? snapshotId,
        int changeIndex,
        IChange? change,
        IObjectBase? entity,
        string typeName,
        string? authorName) : this(commitId,
        entityId,
        new DateTimeOffset(timestamp.Ticks,
            TimeSpan.Zero), //todo this is a workaround for linq2db bug where it reads a date and assumes it's local when it's UTC
        snapshotId,
        changeIndex,
        HistoryService.ChangeNameHelper(change),
        (IObjectWithId?) entity?.DbObject,
        typeName,
        authorName)
    {
    }
}

public class HistoryService(ICrdtDbContext dbContext, DataModel dataModel)
{
    public IAsyncEnumerable<ProjectActivity> ProjectActivity()
    {
        return dbContext.Commits
                .DefaultOrderDescending()
                .Take(100)
                .Select(c => new ProjectActivity(c.Id, c.HybridDateTime.DateTime, c.ChangeEntities, c.Metadata))
                .AsAsyncEnumerable();
    }

    public async Task<ObjectSnapshot?> GetSnapshot(Guid snapshotId)
    {
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

    public IAsyncEnumerable<HistoryLineItem> GetHistory(Guid entityId)
    {
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
            select new HistoryLineItem(commit.Id,
                entityId,
                commit.HybridDateTime.DateTime,
                snapshot.Id,
                change.Index,
                change.Change,
                snapshot.Entity,
                snapshot.TypeName,
                commit.Metadata.AuthorName);
        return query.ToLinqToDB().AsAsyncEnumerable();
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

    [return: NotNullIfNotNull("change")]
    public static string? ChangeNameHelper(IChange? change)
    {
        if (change is null) return null;
        var type = change.GetType();
        //todo call JsonPatchChange.Summarize() instead of this
        if (type.Name.StartsWith("JsonPatchChange")) return $"Change{change.EntityType.Name}".Humanize();
        else if (type.Name.StartsWith("DeleteChange`")) return $"Delete{change.EntityType.Name}".Humanize();
        else if (type.Name.StartsWith("SetOrderChange`")) return $"Reorder{change.EntityType.Name}".Humanize();
        var changeName = type.Name.Humanize();
        return Regex.Replace(changeName, " Change$", "", RegexOptions.IgnoreCase);
    }
}
