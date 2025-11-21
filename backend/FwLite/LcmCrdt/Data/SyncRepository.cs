using System.Diagnostics;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.Data;

public class SyncRepository(IDbContextFactory<LcmCrdtDbContext> dbContextFactory, ILogger<SyncRepository> logger)
{
    /// <summary>
    /// Note this will update any commits, not just the ones that were synced. This includes ours which we just sent
    /// </summary>
    public async Task UpdateSyncDate(DateTimeOffset syncDate)
    {
        try
        {
            //the prop name is hardcoded into the sql so we just want to assert it's what we expect
            Debug.Assert(CommitHelpers.SyncDateProp == "SyncDate");
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Database.ExecuteSqlAsync(
                $"""
                 UPDATE Commits
                 SET metadata = json_set(metadata, '$.ExtraMetadata.SyncDate', {syncDate.ToString("u")})
                 WHERE json_extract(Metadata, '$.ExtraMetadata.SyncDate') IS NULL;
                 """);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update sync date");
        }
    }

    public async Task<int?> CountPendingCommits()
    {
        try
        {
            // Assert sync date prop for same reason as in UpdateSyncDate
            Debug.Assert(CommitHelpers.SyncDateProp == "SyncDate");
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            int count = await dbContext.Database.SqlQuery<int>(
                $"""
                 SELECT COUNT(*) AS Value FROM Commits
                 WHERE json_extract(Metadata, '$.ExtraMetadata.SyncDate') IS NULL
                 """).SingleAsync();
            return count;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to count pending commits");
            return null;
        }
    }

    public async Task<DateTimeOffset?> GetLatestSyncedCommitDate()
    {
        try
        {
            // Assert sync date prop for same reason as in UpdateSyncDate
            Debug.Assert(CommitHelpers.SyncDateProp == "SyncDate");
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var date = await dbContext.Database.SqlQuery<DateTimeOffset?>(
                $"""
                 SELECT MAX(json_extract(Metadata, '$.ExtraMetadata.SyncDate')) AS Value FROM Commits
                 """).SingleAsync();
            return date;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to find most recent commit date");
            return null;
        }
    }
}
