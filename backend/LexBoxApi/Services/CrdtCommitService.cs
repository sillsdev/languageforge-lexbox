using LexCore.Utils;
using LexData;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore.Internal;
using SIL.Harmony.Core;

namespace LexBoxApi.Services;

public class CrdtCommitService(LexBoxDbContext dbContext)
{
    public async Task AddCommits(Guid projectId, IAsyncEnumerable<ServerCommit> commits)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var commitsTable = dbContext.CreateLinqToDBContext().GetTable<ServerCommit>();
        const int commitsThreshold = 100;
        const int changeThreshold = 150;
        var currentChangeCount = 0;
        var commitBucket = new List<ServerCommit>(100);
        await foreach (var serverCommit in commits)
        {
            commitBucket.Add(serverCommit);
            currentChangeCount += serverCommit.ChangeEntities.Count;
            if (currentChangeCount < changeThreshold && commitBucket.Count < commitsThreshold)
            {
                continue;
            }
            //flush commits
            await commitsTable
                .Merge()
                .Using(commitBucket)
                .OnTargetKey()
                .InsertWhenNotMatched(commit => new ServerCommit(commit.Id)
                {
                    Id = commit.Id,
                    ClientId = commit.ClientId,
                    HybridDateTime = new HybridDateTime(commit.HybridDateTime.DateTime, commit.HybridDateTime.Counter)
                    {
                        DateTime = commit.HybridDateTime.DateTime, Counter = commit.HybridDateTime.Counter
                    },
                    ProjectId = projectId,
                    Metadata = commit.Metadata,
                    //without this sql cast the value will be treated as text and fail to insert into the jsonb column
                    ChangeEntities = Sql.Expr<List<ChangeEntity<ServerJsonChange>>>($"{commit.ChangeEntities}::jsonb")
                })
                .MergeAsync();
            commitBucket.Clear();
            currentChangeCount = 0;
        }

        await transaction.CommitAsync();
    }

    public IAsyncEnumerable<ServerCommit> GetMissingCommits(Guid projectId, SyncState localState, SyncState remoteState)
    {
        return dbContext.CrdtCommits(projectId)
        //don't need to include change entities since they're not owned in lexbox so they will get included automatically
            .GetMissingCommits<ServerCommit, ServerJsonChange>(localState, remoteState, false);
    }

    public async Task<SyncState> GetSyncState(Guid projectId)
    {
        return await dbContext.CrdtCommits(projectId).GetSyncState();
    }
}
