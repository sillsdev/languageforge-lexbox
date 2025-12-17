using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SIL.Harmony;
using SIL.Harmony.Db;
using SIL.Harmony.Core;
using Commit = SIL.Harmony.Commit;

namespace LcmCrdt;

public class SnapshotAtCommitService(
    IServiceProvider serviceProvider,
    IOptions<CrdtConfig> crdtConfig,
    CurrentProjectService currentProjectService,
    ILogger<SnapshotAtCommitService> logger,
    ICrdtDbContextFactory crdtDbContextFactory)
{
    private static readonly ActivitySource _activitySource = new("LcmCrdt.SnapshotAtCommitService");
    public async Task<ProjectSnapshot?> GetProjectSnapshotAtCommit(Guid commitId)
    {
        using var activity = _activitySource.StartActivity();
        activity?.SetTag("app.commit_id", commitId);
        var dbContext = await crdtDbContextFactory.CreateDbContextAsync();
        var commit = await dbContext.Commits.SingleOrDefaultAsync(c => c.Id == commitId);
        if (commit is null)
        {
            logger.LogWarning("Commit {CommitId} not found in forked database", commitId);
            return null;
        }

        var projectCode = currentProjectService.Project.Name;
        var forkDbPath = Path.Combine(Path.GetTempPath(), $"fwlite_fork_{projectCode}_{Guid.NewGuid()}.db");

        try
        {
            logger.LogInformation("Forking database to {forkDbPath} for commit {CommitId}", forkDbPath, commitId);
            var dbPath = currentProjectService.Project.DbPath;
            await ForkDatabase(dbPath, forkDbPath);

            var project = new CrdtProject(currentProjectService.Project.Name, forkDbPath);
            var serviceScope = serviceProvider.CreateAsyncScope();
            var scopedCurrentProjectService = serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>();
            var projectData = await scopedCurrentProjectService.SetupProjectContext(project);

            // could pull this out of the new scope, but it's nice making it so explicit,
            // because deleting commits is kinda risky business 🕵️‍♂️
            var options = new DbContextOptionsBuilder<LcmCrdtDbContext>()
                .UseSqlite($"Data Source={forkDbPath}").Options;
            ICrdtDbContext forkDbContext = new LcmCrdtDbContext(options, crdtConfig);

            var deleted = await DeleteCommitsAfter(forkDbContext, commit);
            logger.LogInformation("Deleted {Deleted} commits after {CommitId}", deleted, commitId);

            var dataModel = serviceScope.ServiceProvider.GetRequiredService<DataModel>();
            await dataModel.RegenerateSnapshots();

            var api = serviceScope.ServiceProvider.GetRequiredService<IMiniLcmApi>();
            return await api.TakeProjectSnapshot();
        }
        finally
        {
            if (File.Exists(forkDbPath))
            {
                try
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(forkDbPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete temporary database file {TempFilePath}", forkDbPath);
                }
            }
        }
    }

    private async Task ForkDatabase(string sourcePath, string destPath)
    {
        await using var sourceConnection = new SqliteConnection($"Data Source={sourcePath}");
        await sourceConnection.OpenAsync();
        await using var destConnection = new SqliteConnection($"Data Source={destPath}");
        await destConnection.OpenAsync();
        sourceConnection.BackupDatabase(destConnection);
    }

    private async Task<int> DeleteCommitsAfter(ICrdtDbContext context, Commit targetCommit)
    {
        var commitsToDelete = context.Commits.WhereAfter(targetCommit);
        context.Set<Commit>().RemoveRange(commitsToDelete);
        await context.SaveChangesAsync();
        return commitsToDelete.Count();
    }
}
