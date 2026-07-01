using Humanizer;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MiniLcm.Exceptions;
using LinqToDB.Async;

namespace LcmCrdt;

public record ActivityAuthor(string? AuthorId, string? AuthorName, int CommitCount);

public record ActivityChangeType(string Key, string Label, int CommitCount);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivitySort
{
    NewestFirst = 0,
    OldestFirst = 1,
    SyncedNewestFirst = 2,
    SyncedOldestFirst = 3,
}

public record ActivityQuery(
    string[]? AuthorFilterKeys = null,
    string[]? ChangeTypeKeys = null,
    ActivitySort Sort = ActivitySort.NewestFirst);

public static class ActivityFilterKeys
{
    public const string UnknownAuthor = "__unknown__";
    public const string AuthorNamePrefix = "name:";
}

/// <summary>
/// Resolved display info for one change, so the frontend can name what the change is about.
/// <paramref name="Subject"/> is the entity the change is on (entry headword, "headword › gloss" for a sense, or a vocab object's name);
/// <paramref name="Target"/> is a referenced item the change names only by id (e.g. the part of speech assigned, the semantic domain removed).
/// Both are null when unresolved — the frontend falls back to a type label.
/// </summary>
public record ActivityChangeInfo(string? Subject, Guid? RootEntryId, string? Target = null);

public record ProjectActivity(
    Guid CommitId,
    DateTimeOffset Timestamp,
    List<ChangeEntity<IChange>> Changes,
    CommitMetadata Metadata)
{
    public string ChangeName => HistoryService.ChangesNameHelper(Changes);
    public string[] ChangeTypes { get; } = Changes.Select(c => HistoryService.GetChangeTypeKey(c.Change)).Distinct().ToArray();
    // Humanized version of ChangeTypes for display ("AddEntryComponentChange" → "Add entry component").
    // Kept alongside the raw keys because ChangeTypes is also used as the filter identifier and must stay stable.
    public string[] ChangeTypeLabels { get; } = Changes
        .Select(c => HistoryService.ChangeNameHelper(c.Change))
        .Distinct()
        .ToArray();
    /// <summary>Resolved display info per change, parallel to <see cref="Changes"/> by index. Set during enrichment.</summary>
    public IReadOnlyList<ActivityChangeInfo> ChangeInfo { get; set; } = [];
}

public record ChangeContext(
    Guid CommitId,
    int ChangeIndex,
    string ChangeName,
    IObjectWithId? Snapshot,
    IObjectWithId? PreviousSnapshot,
    ICollection<Entry> AffectedEntries)
{
    public ChangeContext(ChangeEntity<IChange> change, IObjectWithId? snapshot, IObjectWithId? previousSnapshot, ICollection<Entry> affectedEntries)
        : this(change.CommitId, change.Index, HistoryService.ChangeNameHelper(change.Change), snapshot, previousSnapshot, affectedEntries)
    {
    }
    public string EntityType => (Snapshot ?? PreviousSnapshot)?.GetType().Name ?? "Unknown";
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

    public async Task<ActivityAuthor[]> ListActivityAuthors()
    {
        await using ICrdtDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var authors = await dbContext.Commits
            .GroupBy(c => new
            {
                AuthorId = Json.Value(c.Metadata, m => m.AuthorId),
                AuthorName = Json.Value(c.Metadata, m => m.AuthorName),
            })
            .Select(g => new ActivityAuthor(g.Key.AuthorId, g.Key.AuthorName, g.Count()))
            .ToListAsyncLinqToDB();
        return authors.OrderBy(a => a.AuthorName ?? "").ThenBy(a => a.AuthorId ?? "").ToArray();
    }

    public async Task<ActivityChangeType[]> ListActivityChangeTypes()
    {
        await using ICrdtDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var changeCounts = await dbContext.Set<ChangeEntity<IChange>>()
            .GroupBy(c => new
            {
                ChangeTypeKey = Sql.Expr<string>("json_extract({0}, '$.\"$type\"')", c.Change)
            })
            .Select(g => new KeyValuePair<string, int>(g.Key.ChangeTypeKey, g.Count()))
            .ToDictionaryAsyncLinqToDB(p => p.Key, p => p.Value);

        var registeredTypes = LcmCrdtKernel.AllChangeTypes()
            .Select(t => new ActivityChangeType(
                GetChangeTypeKeyFromType(t),
                ChangeTypeLabel(t),
                changeCounts.GetValueOrDefault(GetChangeTypeKeyFromType(t))))
            .Where(t => t.CommitCount > 0)
            .OrderBy(t => t.Label)
            .ToArray();

        return registeredTypes;
    }

    public async IAsyncEnumerable<ProjectActivity> ProjectActivity(int skip = 0, int take = 100, ActivityQuery? query = null)
    {
        query ??= new ActivityQuery();
        await using ICrdtDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var commits = ApplyActivityFilters(dbContext.Commits, query);
        commits = ApplyActivitySort(commits, query.Sort);
        var queryable =
            from commit in commits.Skip(skip).Take(take)
            select new ProjectActivity(commit.Id,
                NormalizeTimestamp(commit.HybridDateTime.DateTime),
                commit.ChangeEntities.ToList(),
                commit.Metadata);
        // Materialize the whole page before resolving: ActivityChangeInfoResolver batch-loads labels across all
        // changes in the page at once, so this enumerates the page rather than streaming row-by-row.
        var activities = new List<ProjectActivity>();
        await foreach (var projectActivity in queryable.ToLinqToDB().AsAsyncEnumerable())
        {
            activities.Add(projectActivity);
        }
        await ActivityChangeInfoResolver.ResolveAsync(dbContext, activities);
        foreach (var projectActivity in activities)
        {
            yield return projectActivity;
        }
    }

    private static IQueryable<Commit> ApplyActivityFilters(IQueryable<Commit> commits, ActivityQuery query)
    {
        if (query.AuthorFilterKeys is { Length: 0 })
        {
            return commits.ToLinqToDB().Where(_ => false);
        }

        if (query.AuthorFilterKeys is { Length: > 0 })
        {
            var authorIds = new List<string>();
            var authorNames = new List<string>();
            var includeUnknown = false;
            foreach (var key in query.AuthorFilterKeys)
            {
                if (key == ActivityFilterKeys.UnknownAuthor)
                    includeUnknown = true;
                else if (key.StartsWith(ActivityFilterKeys.AuthorNamePrefix, StringComparison.Ordinal))
                    authorNames.Add(key[ActivityFilterKeys.AuthorNamePrefix.Length..]);
                else
                    authorIds.Add(key);
            }

            commits = commits.ToLinqToDB().Where(c =>
                (includeUnknown
                 && (Json.Value(c.Metadata, m => m.AuthorId) ?? "") == ""
                 && (Json.Value(c.Metadata, m => m.AuthorName) ?? "") == "")
                || authorIds.Contains(Json.Value(c.Metadata, m => m.AuthorId) ?? "")
                || authorNames.Contains(Json.Value(c.Metadata, m => m.AuthorName) ?? ""));
        }

        if (query.ChangeTypeKeys is { Length: 0 })
        {
            return commits.ToLinqToDB().Where(_ => false);
        }

        if (query.ChangeTypeKeys is { Length: > 0 })
        {
            var changeTypeKeys = query.ChangeTypeKeys;
            commits = commits.ToLinqToDB().Where(c => c.ChangeEntities
                .Any(ce => changeTypeKeys.Contains(Sql.Expr<string>("json_extract({0}, '$.\"$type\"')", ce.Change)))
            );
        }

        return commits;
    }

    private static IQueryable<Commit> ApplyActivitySort(IQueryable<Commit> commits, ActivitySort sort)
    {
        return sort switch
        {
            ActivitySort.OldestFirst => commits.DefaultOrder(),
            ActivitySort.SyncedNewestFirst => commits.ToLinqToDB()
                .OrderByDescending(c => Sql.Expr<int>(
                    "CASE WHEN json_extract({0}, '$.ExtraMetadata.SyncDate') IS NULL THEN 1 ELSE 0 END", c.Metadata))
                .ThenByDescending(c => Sql.Expr<string>(
                    "json_extract({0}, '$.ExtraMetadata.SyncDate')", c.Metadata))
                .ThenByDescending(c => c.HybridDateTime.DateTime)
                .ThenByDescending(c => c.HybridDateTime.Counter)
                .ThenByDescending(c => c.Id),
            ActivitySort.SyncedOldestFirst => commits.ToLinqToDB()
                .OrderBy(c => Sql.Expr<int>(
                    "CASE WHEN json_extract({0}, '$.ExtraMetadata.SyncDate') IS NULL THEN 1 ELSE 0 END", c.Metadata))
                .ThenBy(c => Sql.Expr<string>(
                    "json_extract({0}, '$.ExtraMetadata.SyncDate')", c.Metadata))
                .ThenBy(c => c.HybridDateTime.DateTime)
                .ThenBy(c => c.HybridDateTime.Counter)
                .ThenBy(c => c.Id),
            _ => commits.DefaultOrderDescending(),
        };
    }

    private static string GetChangeTypeKeyFromType(Type changeType)
    {
        var typeNameProp = changeType.GetProperty("TypeName",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
        if (typeNameProp?.GetValue(null) is string name)
            return name;
        return changeType.Name;
    }

    internal static string GetChangeTypeKey(IChange change) =>
        GetChangeTypeKeyFromType(change.GetType());

    public static string ChangeTypeLabel(Type changeType)
    {
        if (changeType.IsGenericType && changeType.Name.Contains("JsonPatch", StringComparison.Ordinal))
            return $"Edit{changeType.GetGenericArguments()[0].Name}".Humanize();
        if (changeType.IsGenericType && changeType.GetGenericTypeDefinition() == typeof(DeleteChange<>))
            return $"Delete{changeType.GetGenericArguments()[0].Name}".Humanize();
        if (changeType.IsGenericType && changeType.Name.StartsWith("SetOrderChange", StringComparison.Ordinal))
            return $"Reorder{changeType.GetGenericArguments()[0].Name}".Humanize();
        var changeName = changeType.Name.Humanize();
        return Regex.Replace(changeName, " Change$", "", RegexOptions.IgnoreCase);
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

        // Use safe cast - some entity types like RemoteResource don't implement IObjectWithId
        var snapshot = await dataModel.GetAtCommit<object>(commitId, change.EntityId) as IObjectWithId;
        var previousSnapshot = await LoadPreviousSnapshot(crdtDbContext, commitId, change.EntityId);

        await ResolveSensePartOfSpeech(snapshot);
        await ResolveSensePartOfSpeech(previousSnapshot);

        var affectedEntries = await GetAffectedEntryIds(change)
            .Select(async (Guid entryId, CancellationToken _) => await GetCurrentOrLatestEntry(entryId))
            .ToArrayAsync();

        return new ChangeContext(change, snapshot, previousSnapshot, affectedEntries);
    }

    /// <summary>The entity's state just before <paramref name="commitId"/>, i.e. the snapshot at the most recent
    /// commit that affected it before this one. Null if this commit created the entity.</summary>
    private async Task<IObjectWithId?> LoadPreviousSnapshot(ICrdtDbContext dbContext, Guid commitId, Guid entityId)
    {
        var affectingCommitIds = await dbContext.Commits
            .Where(c => c.ChangeEntities.Any(ce => ce.EntityId == entityId))
            .DefaultOrderDescending()
            .Select(c => c.Id)
            .ToListAsyncLinqToDB();
        var index = affectingCommitIds.IndexOf(commitId);
        if (index < 0 || index + 1 >= affectingCommitIds.Count) return null;
        return await dataModel.GetAtCommit<object>(affectingCommitIds[index + 1], entityId) as IObjectWithId;
    }

    // Older sense snapshots didn't store the part-of-speech object, only its id; resolve it so previews can show a label.
    private async Task ResolveSensePartOfSpeech(IObjectWithId? snapshot)
    {
        if (snapshot is Sense sense && sense.PartOfSpeechId != sense.PartOfSpeech?.Id)
        {
            sense.PartOfSpeech = sense.PartOfSpeechId.HasValue
                ? await dataModel.GetLatest<PartOfSpeech>(sense.PartOfSpeechId.Value)
                : null;
        }
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
