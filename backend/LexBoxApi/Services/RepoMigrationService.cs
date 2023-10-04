using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LexBoxApi.Otel;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexCore.Utils;
using LexData;
using Microsoft.EntityFrameworkCore;
using Nito.AsyncEx;

namespace LexBoxApi.Services;

public class RepoMigrationService : BackgroundService, IRepoMigrationService
{
    public static TimeSpan QueueAfterReadBlockDelay { get; set; } = TimeSpan.FromSeconds(30);
    private readonly IServiceProvider _serviceProvider;

    public RepoMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private Channel<string> ProjectMigrationQueue { get; } = Channel.CreateUnbounded<string>();
    private readonly Channel<string> _migrationCompleted = Channel.CreateUnbounded<string>();
    public ChannelReader<string> MigrationCompleted => _migrationCompleted.Reader;

    private readonly TaskCompletionSource _started = new();
    /// used for tests to determine that the projects have been queried from the db on startup
    public Task Started => _started.Task;

    public void QueueMigration(string projectCode)
    {
        ProjectMigrationQueue.Writer.TryWrite(projectCode);
    }

    private readonly ConcurrentWeakDictionary<string, AsyncReaderWriterLock> _projectLocks = new();
    private readonly CancellationToken _cancelled = new(true);

    /// <summary>
    /// Notify that a project is being sent & received. This will block migration from starting. It will return null if a migration has started indicating that send receive should be blocked
    /// </summary>
    /// <returns>null if you shouldn't start, a disposable to dispose when you're done</returns>
    public async ValueTask<IDisposable?> BeginSendReceive(string projectCode, ProjectMigrationStatus status = ProjectMigrationStatus.PublicRedmine)
    {
        //if the project is already migrated, we don't need to block
        if (status is ProjectMigrationStatus.Migrated) return Defer.Noop;
        var projectLock = _projectLocks.GetOrAdd(projectCode, _ => new AsyncReaderWriterLock());
        var result = projectLock.ReaderLockAsync(_cancelled);
        //task will be cancelled if the lock is already held
        if (result.AsTask().IsCanceled) return null;
        return await result;
    }

    private async Task<IDisposable?> BlockSendReceive(string projectCode)
    {
        var projectLock = _projectLocks.GetOrAdd(projectCode, _ => new AsyncReaderWriterLock());
        var result = projectLock.WriterLockAsync(_cancelled);
        if (result.AsTask().IsCanceled) return null;
        return await result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //load currently migrating projects into the queue
        await using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LexBoxDbContext>();
            var migratingProjects = await dbContext.Projects
                .Where(p => p.MigrationStatus == ProjectMigrationStatus.Migrating).Select(p => p.Code)
                .ToArrayAsync(stoppingToken);
            foreach (var projectCode in migratingProjects)
            {
                await ProjectMigrationQueue.Writer.WriteAsync(projectCode, stoppingToken);
            }
        }

        _started.SetResult();
        while (!stoppingToken.IsCancellationRequested)
        {
            bool migrated;
            var projectCode = await ProjectMigrationQueue.Reader.ReadAsync(stoppingToken);
            using (var blockSendReceive = await BlockSendReceive(projectCode))
            {
                if (blockSendReceive is null)
                {
                    //add the project back to the queue after a delay, otherwise we might just spin on this project
                    var _ = Task.Run(async () =>
                    {
                        await Task.Delay(QueueAfterReadBlockDelay, stoppingToken);
                        QueueMigration(projectCode);
                    }, stoppingToken);
                    continue;
                }

                migrated = await MigrateProject(projectCode, stoppingToken).ConfigureAwait(false);
            }

            if (migrated)
            {
                await _migrationCompleted.Writer.WriteAsync(projectCode, stoppingToken);
            }
        }
    }

    private async Task<bool> MigrateProject(string projectCode, CancellationToken stoppingToken)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag("app.project_code", projectCode);
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LexBoxDbContext>();
        var hgService = scope.ServiceProvider.GetRequiredService<IHgService>();
        var project = await dbContext.Projects.SingleAsync(p => p.Code == projectCode, stoppingToken);
        if (project.MigrationStatus == ProjectMigrationStatus.Migrated)
        {
            return true;
        }

        if (await hgService.MigrateRepo(project, stoppingToken))
        {
            project.MigrationStatus = ProjectMigrationStatus.Migrated;
            await dbContext.SaveChangesAsync(stoppingToken);
            return true;
        }

        return false;
    }
}
