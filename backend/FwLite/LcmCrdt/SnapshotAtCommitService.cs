using SIL.Harmony.Config;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SIL.Harmony;
using SIL.Harmony.Db;
using SIL.Harmony.Core;
using LinqToDB.EntityFrameworkCore;

namespace LcmCrdt;

public class SnapshotAtCommitService(
    IServiceProvider serviceProvider,
    IOptions<HarmonyConfig> harmonyConfig,
    CurrentProjectService currentProjectService,
    ILogger<SnapshotAtCommitService> logger,
    ICrdtDbContextFactory crdtDbContextFactory)
{
    private static readonly ActivitySource _activitySource = new("LcmCrdt.SnapshotAtCommitService");
    public async Task<ProjectSnapshot?> GetProjectSnapshotAtCommit(Guid commitId, bool preserveAllFieldWorksCommits = false)
    {
        using var activity = _activitySource.StartActivity();
        activity?.SetTag("app.commit_id", commitId);
        await using var dbContext = await crdtDbContextFactory.CreateDbContextAsync();
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
            await using var serviceScope = serviceProvider.CreateAsyncScope();
            var scopedCurrentProjectService = serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>();
            var projectData = await scopedCurrentProjectService.SetupProjectContext(project);

            // could pull this out of the new scope, but it's nice making it so explicit,
            // because deleting commits is kinda risky business 🕵️‍♂️
            var optionsBuilder = new DbContextOptionsBuilder<LcmCrdtDbContext>()
                .UseSqlite($"Data Source={forkDbPath}");
            LcmCrdtKernel.ConfigureDbOptions(serviceScope.ServiceProvider, optionsBuilder);
            await using var forkDbContext = new LcmCrdtDbContext(optionsBuilder.Options, harmonyConfig);

            var deleted = await DeleteCommitsAfter(forkDbContext, commit, preserveAllFieldWorksCommits);
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
                    using var clearConn = new SqliteConnection($"Data Source={forkDbPath}");
                    SqliteConnection.ClearPool(clearConn);
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

    private async Task<int> DeleteCommitsAfter(ICrdtDbContext context, Commit targetCommit, bool preserveAllFieldWorksCommits)
    {
        // WhereAfter must run through EF (like Harmony's own CrdtRepository): linq2db wraps SQLite timestamp
        // comparisons in strftime, which is millisecond-grained, so it cannot order commits exactly.
        // Only the AuthorName lookup needs linq2db (Json.Value has no EF translation), and it filters on
        // commit ids, not timestamps.
        var idsAfter = await context.Commits.WhereAfter(targetCommit).Select(c => c.Id).ToArrayAsync();
        if (preserveAllFieldWorksCommits)
        {
            var fieldWorksIds = await context.Commits.ToLinqToDB()
                .Where(c => idsAfter.Contains(c.Id) &&
                    // JSON Sqlite gotcha: null != "FieldWorks" == false (apparently)
                    (Json.Value(c.Metadata, m => m.AuthorName) ?? "") == "FieldWorks")
                .Select(c => c.Id)
                .ToArrayAsyncLinqToDB();
            idsAfter = idsAfter.Except(fieldWorksIds).ToArray();
        }
        context.Set<Commit>().RemoveRange(context.Set<Commit>().Where(c => idsAfter.Contains(c.Id)));
        await context.SaveChangesAsync();
        return idsAfter.Length;
    }
}
