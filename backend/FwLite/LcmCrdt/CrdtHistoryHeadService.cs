using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Db;

namespace LcmCrdt;

/// <summary>
/// The head of this project's CRDT history. Anything that persists a state derived from the CRDT (the sync's
/// merge base) stamps the head so the record can later be tested against the history it came from.
/// </summary>
public class CrdtHistoryHeadService(IServiceProvider services)
{
    // Building a db context's options reads CurrentProjectService.Project, so the factory can't be a constructor
    // dependency: this service is resolved before the project context is set up. Same reason as CurrentProjectService.
    private ICrdtDbContextFactory DbContextFactory => services.GetRequiredService<ICrdtDbContextFactory>();

    public async Task<Guid?> GetHeadCommitId()
    {
        return (await GetHeadCommit())?.Id;
    }

    public async Task<Commit?> GetHeadCommit()
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        return await dbContext.Commits.DefaultOrderDescending().FirstOrDefaultAsync();
    }

    public async Task<Commit?> FindCommit(Guid commitId)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        return await dbContext.Commits.SingleOrDefaultAsync(c => c.Id == commitId);
    }

    /// <summary>
    /// Commits ordered after <paramref name="commit"/>, capped so a wildly diverged history doesn't have to be
    /// materialised to be recognised as wildly diverged.
    /// </summary>
    public async Task<Commit[]> GetCommitsAfter(Commit commit, int limit)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        // WhereAfter must run through EF, not linq2db: linq2db wraps SQLite timestamp comparisons in strftime,
        // which is millisecond-grained and can't order commits exactly. See SnapshotAtCommitService.
        return await dbContext.Commits.WhereAfter(commit).DefaultOrder().Take(limit).ToArrayAsync();
    }
}
