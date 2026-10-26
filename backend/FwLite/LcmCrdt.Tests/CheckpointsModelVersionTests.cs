using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SIL.Harmony;
using SIL.Harmony.Db;

namespace LcmCrdt.Tests;

public class CheckpointsModelVersionTests
{
    [Fact]
    public async Task OpeningADbFromBeforeCheckpointsRebuildsSnapshotsOnce()
    {
        var code = $"before-checkpoints-{Guid.NewGuid():N}";
        var oldCopyCode = $"{code}-old";
        var upgradedCopyCode = $"{code}-upgraded";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        var host = builder.Build();
        var projectPath = host.Services.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        var projectsService = host.Services.GetRequiredService<CrdtProjectsService>();
        string DbPath(string projectCode) => Path.Combine(projectPath, $"{projectCode}.sqlite");
        try
        {
            var project = await projectsService.CreateExampleProject(code);
            int entryCount;
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var api = await scope.ServiceProvider.OpenCrdtProject(project);
                entryCount = await api.CountEntries();
                entryCount.Should().BePositive();
                await using var dbContext = await CreateDbContext(scope.ServiceProvider);
                await dbContext.Database.ExecuteSqlRawAsync("UPDATE Commits SET IsSnapshotCheckpoint = 0");
                await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM LocalState WHERE Key = 'harmony:modelVersions'");
                // A copy has a new db path, so it isn't skipped by the once-per-process migration cache
                await dbContext.Database.ExecuteSqlAsync($"VACUUM INTO {DbPath(oldCopyCode)}");
            }

            await using (var scope = host.Services.CreateAsyncScope())
            {
                var api = await scope.ServiceProvider.OpenCrdtProject(GetProject(projectsService, oldCopyCode));
                (await api.CountEntries()).Should().Be(entryCount);
                await using var dbContext = await CreateDbContext(scope.ServiceProvider);
                (await dbContext.Set<Commit>().AnyAsync(c => c.IsSnapshotCheckpoint)).Should().BeTrue();

                // Lets the next open show whether it rebuilt again
                await dbContext.Database.ExecuteSqlRawAsync("UPDATE Commits SET IsSnapshotCheckpoint = 0");
                await dbContext.Database.ExecuteSqlAsync($"VACUUM INTO {DbPath(upgradedCopyCode)}");
            }

            await using (var scope = host.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.OpenCrdtProject(GetProject(projectsService, upgradedCopyCode));
                await using var dbContext = await CreateDbContext(scope.ServiceProvider);
                (await dbContext.Set<Commit>().AnyAsync(c => c.IsSnapshotCheckpoint)).Should().BeFalse();
            }
        }
        finally
        {
            host.Dispose();
            foreach (var projectCode in new[] { code, oldCopyCode, upgradedCopyCode })
            foreach (var file in Directory.EnumerateFiles(projectPath, $"{projectCode}.sqlite*"))
            {
                // Best effort: a pooled connection can still hold the file on Windows
                try { File.Delete(file); } catch (IOException) { }
            }
        }
    }

    [Fact]
    public async Task CreatingAProjectKeepsTheSnapshotsItWrote()
    {
        var code = $"new-project-{Guid.NewGuid():N}";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        Guid[] snapshotIds = [];
        var project = await host.Services.GetRequiredService<CrdtProjectsService>().CreateProjectFromTemplate(
            new(code, code, AfterCreate: async (services, _) =>
            {
                await using var dbContext = await CreateDbContext(services);
                snapshotIds = await ((ICrdtDbContext)dbContext).Snapshots.Select(s => s.Id).ToArrayAsync();
            }),
            vernacularWs: "de");

        await using var scope = host.Services.CreateAsyncScope();
        await scope.ServiceProvider.OpenCrdtProject(project);
        await using var dbContext = await CreateDbContext(scope.ServiceProvider);
        snapshotIds.Should().NotBeEmpty();
        (await ((ICrdtDbContext)dbContext).Snapshots.CountAsync(s => snapshotIds.Contains(s.Id)))
            .Should().Be(snapshotIds.Length);
        await dbContext.Database.EnsureDeletedAsync();
    }

    private static CrdtProject GetProject(CrdtProjectsService projectsService, string code) =>
        projectsService.GetProject(code) ?? throw new InvalidOperationException($"Project '{code}' not found");

    private static async Task<LcmCrdtDbContext> CreateDbContext(IServiceProvider services) =>
        await services.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
}
