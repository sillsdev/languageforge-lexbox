using LexCore.Utils;
using LexData;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore.Internal;
using SIL.Harmony.Core;

namespace LexBoxApi.Services;

public class CrdtCommitService(LexBoxDbContext dbContext)
{
    /// <returns>how many commits were new; commits whose id is already stored are skipped</returns>
    public async Task<int> AddCommits(Guid projectId, IAsyncEnumerable<ServerCommit> commits, CancellationToken token = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
        var linqToDbContext = dbContext.CreateLinqToDBContext();
        await using var tmpTable = await linqToDbContext.CreateTempTableAsync<ServerCommit>($"tmp_crdt_commit_import_{projectId}__{Guid.NewGuid()}", cancellationToken: token);
        //Stamp ProjectId while streaming so the merge below can be a plain column-to-column copy.
        //A projection lambda here would let linq2db v6 wrap our Sql.Expr<...>::jsonb cast in the
        //EF value-converter (JsonSerializer.Serialize) and fail SQL translation.
        var stampedCommits = commits.Select(c => { c.ProjectId = projectId; return c; });
        //rows stream into COPY either way; a small MaxBatchSize only adds a server round trip per batch
        await tmpTable.BulkCopyAsync(new BulkCopyOptions{BulkCopyType = BulkCopyType.ProviderSpecific}, stampedCommits, token);

        var commitsTable = linqToDbContext.GetTable<ServerCommit>();
        var inserted = await commitsTable
            .Merge()
            .Using(tmpTable)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .MergeAsync(token);

        await transaction.CommitAsync(token);
        return inserted;
    }

    public IAsyncEnumerable<ServerCommit> GetMissingCommits(Guid projectId, SyncState localState, SyncState remoteState)
    {
        return dbContext.CrdtCommits(projectId)
        //don't need to include change entities since they're not owned in lexbox so they will get included automatically
            .GetCommitsMissingFromRemote<ServerCommit, ServerJsonChange>(localState, remoteState, false);
    }

    public async Task<int> ApproximatelyCountMissingCommits(Guid projectId, SyncState localState, SyncState remoteState)
    {
        var linqToDbContext = dbContext.CreateLinqToDBContext();
        var commits = linqToDbContext.GetTable<ServerCommit>().Where(c => c.ProjectId == projectId);
        var count = 0;
        //mirrors the send decision in Harmony's QueryHelpers.GetCommitsMissingFromRemote
        foreach (var local in localState.ClientStates)
        {
            var remote = remoteState.GetClientState(local.ClientId);
            //client is new to the other history
            if (remote is null)
            {
                count += local.CommitCount;
            }
            //client has newer history than the other history, an older peer only reports its timestamp
            else if (local.MaxTimestamp > remote.MaxTimestamp)
            {
                var otherDt = DateTimeOffset.FromUnixTimeMilliseconds(remote.MaxTimestamp);
                count += await commits
                    .Where(c => c.ClientId == local.ClientId && c.HybridDateTime.DateTime > otherDt)
                    .CountAsync();
            }
            //same head but different commits, the whole client history is sent
            else if (!remote.OnlyHasTimestamp && local != remote && local.CommitCount >= remote.CommitCount)
            {
                count += local.CommitCount;
            }
        }
        return count;
    }

    public async Task<SyncState> GetSyncState(Guid projectId)
    {
        return await dbContext.CrdtCommits(projectId).GetSyncState();
    }

    /// <summary>
    /// Adds an empty commit dated before every other commit, which makes clients replay their whole
    /// history and rebuild their snapshots from scratch. Repeatable: each call adds another commit.
    /// </summary>
    /// <returns>the commit added, or null if the project has no commits</returns>
    public async Task<SnapshotRebuildCommit?> AddSnapshotRebuildCommit(Guid projectId,
        string note,
        CancellationToken token = default)
    {
        var linqToDbContext = dbContext.CreateLinqToDBContext();
        var commits = linqToDbContext.GetTable<ServerCommit>().Where(c => c.ProjectId == projectId);
        var oldest = await commits.DefaultOrder().FirstOrDefaultAsync(token);
        if (oldest is null) return null;
        var commitsToReplay = await commits.CountAsync(token);

        var startOfDayBeforeOldest = new DateTimeOffset(oldest.HybridDateTime.DateTime.UtcDateTime.Date, TimeSpan.Zero).AddDays(-1);
        var reason = "Forces a full snapshot rebuild on all clients";
        var commit = new ServerCommit(Guid.NewGuid())
        {
            ProjectId = projectId,
            ClientId = Guid.NewGuid(),
            HybridDateTime = new HybridDateTime(startOfDayBeforeOldest, 0),
            Metadata = new CommitMetadata
            {
                AuthorName = "Lexbox maintenance",
                ExtraMetadata = { ["reason"] = $"{reason}. {note}" },
            },
        };
        dbContext.Add(commit);
        await dbContext.SaveChangesAsync(token);
        return new SnapshotRebuildCommit(commit.Id, commit.ClientId, commit.DateTime, commitsToReplay);
    }
}

public record SnapshotRebuildCommit(Guid CommitId, Guid ClientId, DateTimeOffset DateTime, int CommitsToReplay);
