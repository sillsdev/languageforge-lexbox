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
    public async Task AddCommits(Guid projectId, IAsyncEnumerable<ServerCommit> commits, CancellationToken token = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
        var linqToDbContext = dbContext.CreateLinqToDBContext();
        await using var tmpTable = await linqToDbContext.CreateTempTableAsync<ServerCommit>($"tmp_crdt_commit_import_{projectId}__{Guid.NewGuid()}", cancellationToken: token);
        //Stamp ProjectId while streaming so the merge below can be a plain column-to-column copy.
        //A projection lambda here would let linq2db v6 wrap our Sql.Expr<...>::jsonb cast in the
        //EF value-converter (JsonSerializer.Serialize) and fail SQL translation.
        var stampedCommits = commits.Select(c => { c.ProjectId = projectId; return c; });
        await tmpTable.BulkCopyAsync(new BulkCopyOptions{BulkCopyType = BulkCopyType.ProviderSpecific, MaxBatchSize = 10}, stampedCommits, token);

        var commitsTable = linqToDbContext.GetTable<ServerCommit>();
        await commitsTable
            .Merge()
            .Using(tmpTable)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .MergeAsync(token);

        await transaction.CommitAsync(token);
    }

    public IAsyncEnumerable<ServerCommit> GetMissingCommits(Guid projectId, SyncState localState, SyncState remoteState)
    {
        return dbContext.CrdtCommits(projectId)
        //don't need to include change entities since they're not owned in lexbox so they will get included automatically
            .GetMissingCommits<ServerCommit, ServerJsonChange>(localState, remoteState, false);
    }

    public async Task<int> ApproximatelyCountMissingCommits(Guid projectId, SyncState localState, SyncState remoteState)
    {
        var linqToDbContext = dbContext.CreateLinqToDBContext();
        var commits = linqToDbContext.GetTable<ServerCommit>().Where(c => c.ProjectId == projectId);
        var count = 0;
        foreach (var (clientId, localTimestamp) in localState.ClientHeads)
        {
            //client is new to the other history
            if (!remoteState.ClientHeads.TryGetValue(clientId, out var otherTimestamp))
            {
                count += await commits
                    .DefaultOrder()
                    .Where(c => c.ClientId == clientId)
                    .CountAsync();
            }
            //client has newer history than the other history
            else if (localTimestamp > otherTimestamp)
            {
                var otherDt = DateTimeOffset.FromUnixTimeMilliseconds(otherTimestamp);
                count += await commits
                    .DefaultOrder()
                    .Where(c => c.ClientId == clientId && c.HybridDateTime.DateTime > otherDt)
                    .CountAsync();
            }
        }
        return count;
    }

    public async Task<SyncState> GetSyncState(Guid projectId)
    {
        return await dbContext.CrdtCommits(projectId).GetSyncState();
    }

    public record SnapshotRebuildCommit(Guid CommitId, Guid ClientId, DateTimeOffset DateTime, int CommitsToReplay);

    /// <summary>
    /// Adds an empty commit dated before the project's oldest commit. A client that receives a commit
    /// predating its history rolls its snapshots back past the start of history and replays every commit in
    /// one batch, rebuilding its snapshots from scratch. That recovers a project whose sync fails because a
    /// rollback resumed from a stale snapshot, see sillsdev/harmony#105.
    /// Repeatable: each call adds another commit, and each one triggers another rebuild.
    /// </summary>
    /// <returns>the commit that was added, or null if the project has no commits to replay</returns>
    public async Task<SnapshotRebuildCommit?> AddSnapshotRebuildCommit(Guid projectId,
        string? note = null,
        CancellationToken token = default)
    {
        var linqToDbContext = dbContext.CreateLinqToDBContext();
        var commits = linqToDbContext.GetTable<ServerCommit>().Where(c => c.ProjectId == projectId);
        var oldest = await commits.MinAsync(c => (DateTimeOffset?)c.HybridDateTime.DateTime, token);
        if (oldest is null) return null;
        var commitsToReplay = await commits.CountAsync(token);

        var reason = "Forces a full snapshot rebuild on all clients (sillsdev/harmony#105)";
        var commit = new ServerCommit(Guid.NewGuid())
        {
            ProjectId = projectId,
            // a client id nobody has seen: every existing client's sync position is already past this
            // commit's date, so under any of their ids it would never be sent to them
            ClientId = Guid.NewGuid(),
            HybridDateTime = new HybridDateTime(oldest.Value.AddDays(-1), 0),
            Metadata = new CommitMetadata
            {
                AuthorName = "Lexbox maintenance",
                ExtraMetadata = { ["reason"] = note is null ? reason : $"{reason}. {note}" },
            },
        };
        dbContext.Add(commit);
        await dbContext.SaveChangesAsync(token);
        return new SnapshotRebuildCommit(commit.Id, commit.ClientId, commit.DateTime, commitsToReplay);
    }
}
