using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Core;
using SIL.Harmony.Db;

namespace LcmCrdt.LocalSync;

public class ProjectIdentityService(CurrentProjectService currentProjectService, IDbContextFactory<LcmCrdtDbContext> dbContextFactory)
{
    /// <summary>
    /// Reads the identity of the current project for comparison with another copy before a local sync.
    /// </summary>
    public async Task<ProjectIdentity> GetIdentity()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        // Harmony's default order (timestamp, then counter, then id) is deterministic across copies,
        // and a synced copy can't author a commit that sorts before the commits it received.
        var rootCommitId = await ((ICrdtDbContext)dbContext).Commits.DefaultOrder()
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
        var wsIds = await dbContext.WritingSystems.Select(ws => ws.WsId).ToArrayAsync();
        return new ProjectIdentity(currentProjectService.ProjectData.Id,
            rootCommitId,
            wsIds.Select(wsId => wsId.Code).ToHashSet());
    }

    public async Task<bool> HasCommit(Guid commitId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await ((ICrdtDbContext)dbContext).Commits.AnyAsync(c => c.Id == commitId);
    }
}
