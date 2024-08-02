using System.Threading.Channels;
using SIL.Harmony;
using LcmCrdt;
using LocalWebApp.Auth;
using Microsoft.Extensions.Options;
using MiniLcm;

namespace LocalWebApp;

public class BackgroundSyncService(
    IServiceProvider serviceProvider,
    ProjectsService projectsService,
    IHostApplicationLifetime applicationLifetime,
    IOptions<AuthConfig>options,
    ProjectContext projectContext) : BackgroundService
{
    private readonly Channel<CrdtProject> _syncResultsChannel = Channel.CreateUnbounded<CrdtProject>();

    public void TriggerSync()
    {
        _syncResultsChannel.Writer.TryWrite(projectContext.Project ??
                                            throw new InvalidOperationException("No project selected"));
    }

    private Task StartedAsync()
    {
        var tcs = new TaskCompletionSource();
        applicationLifetime.ApplicationStarted.Register(() => tcs.SetResult());
        return tcs.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //need to wait until application is started, otherwise Server urls will be unknown which prevents creating downstream services
        await StartedAsync();
        var crdtProjects = await projectsService.ListProjects();
        foreach (var crdtProject in crdtProjects)
        {
            await SyncProject(crdtProject);
        }

        await foreach (var project in _syncResultsChannel.Reader.ReadAllAsync(stoppingToken))
        {
            //todo, this might not be required, but I can't remember why I added it
            await Task.Delay(100, stoppingToken);
            await SyncProject(project);
        }
    }

    private async Task<SyncResults> SyncProject(CrdtProject crdtProject)
    {
        await using var serviceScope = projectsService.CreateProjectScope(crdtProject);
        var syncService = serviceScope.ServiceProvider.GetRequiredService<SyncService>();
        return await syncService.ExecuteSync();
    }
}
