using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SIL.Harmony;

namespace LcmCrdt.Tests;

public class OpenProjectRebuildsSnapshotsTests
{
    private class RecordingListener : ISnapshotRebuildListener
    {
        public List<string> Events { get; } = [];
        public void RebuildStarted(CrdtProject project) => Events.Add($"started:{project.Name}");
        public void RebuildFinished(CrdtProject project) => Events.Add($"finished:{project.Name}");
    }

    [Fact]
    public async Task OpeningADbWithoutCheckpointsRebuildsSnapshotsOnce()
    {
        var code = $"no-checkpoints-{Guid.NewGuid():N}";
        var copyCode = $"{code}-copy";
        var rebuiltCopyCode = $"{code}-rebuilt-copy";
        var listener = new RecordingListener();
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        builder.Services.AddSingleton<ISnapshotRebuildListener>(listener);
        var host = builder.Build();
        var projectPath = host.Services.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        try
        {
            var projectsService = host.Services.GetRequiredService<CrdtProjectsService>();
            var project = await projectsService.CreateExampleProject(code);

            int entryCount;
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var api = await scope.ServiceProvider.OpenCrdtProject(project);
                entryCount = await api.CountEntries();
                entryCount.Should().BePositive();
                // A fresh project already has checkpoints, so nothing is rebuilt
                listener.Events.Should().BeEmpty();

                // Simulate a db written before checkpoints existed, as the copy keeps the WAL content
                await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
                await dbContext.Database.ExecuteSqlRawAsync("UPDATE Commits SET IsSnapshotCheckpoint = 0");
                await dbContext.Database.ExecuteSqlRawAsync($"VACUUM INTO '{Path.Combine(projectPath, $"{copyCode}.sqlite")}'");
            }

            var copy = projectsService.GetProject(copyCode) ?? throw new InvalidOperationException("copy not found");
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var api = await scope.ServiceProvider.OpenCrdtProject(copy);
                listener.Events.Should().Equal($"started:{copyCode}", $"finished:{copyCode}");

                await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
                (await dbContext.Set<Commit>().AnyAsync(c => c.IsSnapshotCheckpoint)).Should().BeTrue();
                (await api.CountEntries()).Should().Be(entryCount);
            }

            // Opening again in the same process does not rebuild a second time
            await using (var scope = host.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.OpenCrdtProject(copy);
                listener.Events.Should().HaveCount(2);
            }

            // Nor does a later process: the rebuild left checkpoints behind (a fresh db path means a fresh migration cache)
            await using (var scope = host.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.OpenCrdtProject(copy);
                await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
                await dbContext.Database.ExecuteSqlRawAsync($"VACUUM INTO '{Path.Combine(projectPath, $"{rebuiltCopyCode}.sqlite")}'");
            }
            var rebuiltCopy = projectsService.GetProject(rebuiltCopyCode) ?? throw new InvalidOperationException("rebuilt copy not found");
            await using (var scope = host.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.OpenCrdtProject(rebuiltCopy);
                listener.Events.Should().HaveCount(2);
            }
        }
        finally
        {
            host.Dispose();
            foreach (var projectCode in new[] { code, copyCode, rebuiltCopyCode })
            foreach (var file in Directory.EnumerateFiles(projectPath, $"{projectCode}.sqlite*"))
            {
                // Best effort: a pooled connection can still hold the file on Windows
                try { File.Delete(file); } catch (IOException) { }
            }
        }
    }
}
