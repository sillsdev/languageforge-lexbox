﻿using System.Collections.Concurrent;
using System.Threading.Channels;
using LexBoxApi.Otel;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class RepoMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RepoMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private Channel<string> ProjectMigrationQueue { get; } = Channel.CreateUnbounded<string>();
    private readonly TaskCompletionSource _started = new();
    /// used for tests to determine that the projects have been queried from the db on startup
    public Task Started => _started.Task;

    public void QueueMigration(string projectCode)
    {
        ProjectMigrationQueue.Writer.TryWrite(projectCode);
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
            var projectCode = await ProjectMigrationQueue.Reader.ReadAsync(stoppingToken);
            await MigrateProject(projectCode, stoppingToken);
        }
    }

    private async Task MigrateProject(string projectCode, CancellationToken stoppingToken)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag("app.project_code", projectCode);
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LexBoxDbContext>();
        var hgService = scope.ServiceProvider.GetRequiredService<IHgService>();
        var project = await dbContext.Projects.SingleAsync(p => p.Code == projectCode, stoppingToken);
        if (project.MigrationStatus == ProjectMigrationStatus.Migrated)
        {
            return;
        }

        if (await hgService.MigrateRepo(project, stoppingToken))
        {
            project.MigrationStatus = ProjectMigrationStatus.Migrated;
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
