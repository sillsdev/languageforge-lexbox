using System.Threading.Channels;
using SIL.Harmony;
using LcmCrdt;
using Microsoft.Extensions.Caching.Memory;

namespace LocalWebApp;

public class BackgroundSyncService(
    ProjectsService projectsService,
    IHostApplicationLifetime applicationLifetime,
    ProjectContext projectContext,
    ILogger<BackgroundSyncService> logger,
    IMemoryCache memoryCache) : BackgroundService
{
    private readonly Channel<CrdtProject> _syncResultsChannel = Channel.CreateUnbounded<CrdtProject>();

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

        var crdtProject = projectsService.GetProject(projectData.Name);
        if (crdtProject is null)
        {
            logger.LogWarning("Received project update for unknown project {ProjectName}", projectData.Name);
            return;
        }

        TriggerSync(crdtProject);
    }
    public void TriggerSync()
    {
        TriggerSync(projectContext.Project ?? throw new InvalidOperationException("No project selected"));
    }

    public void TriggerSync(CrdtProject crdtProject)
    {
        _syncResultsChannel.Writer.TryWrite(crdtProject);
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
        await serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
        var syncService = serviceScope.ServiceProvider.GetRequiredService<SyncService>();
        return await syncService.ExecuteSync();
    }
}
