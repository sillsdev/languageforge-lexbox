using System.Threading.Channels;
using LcmCrdt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIL.Harmony;

namespace FwLiteShared.Sync;

public class BackgroundSyncService(
    CrdtProjectsService crdtProjectsService,
    ILogger<BackgroundSyncService> logger,
    IMemoryCache memoryCache,
    IServiceProvider serviceProvider,
    IHostApplicationLifetime? applicationLifetime = null) : BackgroundService
{
    private readonly Channel<CrdtProject> _syncResultsChannel = Channel.CreateUnbounded<CrdtProject>();
    private bool _running = false;

    public void TriggerSync(Guid projectId, Guid? ignoredClientId = null)
    {
        var projectData = CurrentProjectService.LookupProjectById(memoryCache, projectId);
        if (projectData is null)
        {
            logger.LogWarning("Received project update for unknown project {ProjectId}", projectId);
            return;
        }
        if (ignoredClientId == projectData.ClientId)
        {
            logger.LogInformation("Received project update for {ProjectId} triggered by my own change, ignoring", projectId);
            return;
        }

        var crdtProject = crdtProjectsService.GetProject(projectData.Name);
        if (crdtProject is null)
        {
            logger.LogWarning("Received project update for unknown project {ProjectName}", projectData.Name);
            return;
        }

        TriggerSync(crdtProject);
    }

    public void TriggerSync(CrdtProject crdtProject)
    {
        if (!_running) throw new InvalidOperationException("Background sync service is not running");
        _syncResultsChannel.Writer.TryWrite(crdtProject);
    }

    private Task StartedAsync()
    {
        if (applicationLifetime is null) return Task.CompletedTask;
        var tcs = new TaskCompletionSource();
        applicationLifetime.ApplicationStarted.Register(() => tcs.SetResult());
        return tcs.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //need to wait until application is started, otherwise Server urls will be unknown which prevents creating downstream services
        await StartedAsync();
        _running = true;
        var crdtProjects = await crdtProjectsService.ListProjects();
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
        try
        {
            await using var serviceScope = serviceProvider.CreateAsyncScope();
            await serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(crdtProject);
            var syncService = serviceScope.ServiceProvider.GetRequiredService<SyncService>();
            return await syncService.ExecuteSync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error syncing project {ProjectId}", crdtProject.Name);
            return new SyncResults([], [], false);
        }
    }
}
