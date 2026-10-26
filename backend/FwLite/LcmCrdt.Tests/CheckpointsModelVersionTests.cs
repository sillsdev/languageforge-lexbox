using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SIL.Harmony;

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
        try
        {
            var project = await projectsService.CreateExampleProject(code);
            int entryCount;
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var api = await scope.ServiceProvider.OpenCrdtProject(project);
                entryCount = await api.CountEntries();
                entryCount.Should().BePositive();
                await using var dbContext = await CreateDbContext(scope);
                await dbContext.Database.ExecuteSqlRawAsync("UPDATE Commits SET IsSnapshotCheckpoint = 0");
                await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM LocalState WHERE Key = 'harmony:modelVersions'");
                // A copy has a new db path, so it isn't skipped by the once-per-process migration cache
                await dbContext.Database.ExecuteSqlRawAsync($"VACUUM INTO '{Path.Combine(projectPath, $"{oldCopyCode}.sqlite")}'");
            }

            await using (var scope = host.Services.CreateAsyncScope())
            {
                var api = await scope.ServiceProvider.OpenCrdtProject(GetProject(projectsService, oldCopyCode));
                (await api.CountEntries()).Should().Be(entryCount);
                await using var dbContext = await CreateDbContext(scope);
                (await dbContext.Set<Commit>().AnyAsync(c => c.IsSnapshotCheckpoint)).Should().BeTrue();

                // Lets the next open show whether it rebuilt again
                await dbContext.Database.ExecuteSqlRawAsync("UPDATE Commits SET IsSnapshotCheckpoint = 0");
                await dbContext.Database.ExecuteSqlRawAsync($"VACUUM INTO '{Path.Combine(projectPath, $"{upgradedCopyCode}.sqlite")}'");
            }

            await using (var scope = host.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.OpenCrdtProject(GetProject(projectsService, upgradedCopyCode));
                await using var dbContext = await CreateDbContext(scope);
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

    private static CrdtProject GetProject(CrdtProjectsService projectsService, string code) =>
        projectsService.GetProject(code) ?? throw new InvalidOperationException($"Project '{code}' not found");

    private static Task<LcmCrdtDbContext> CreateDbContext(AsyncServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
}
