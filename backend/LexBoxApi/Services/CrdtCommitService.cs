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
}
