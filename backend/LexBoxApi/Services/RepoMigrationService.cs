using System.Collections.Concurrent;
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
        var project = dbContext.Projects.Single(p => p.Code == projectCode);
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
