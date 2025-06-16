using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LexCore.Sync;
using LexCore.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

public class SyncHostedService(IServiceProvider services, ILogger<SyncHostedService> logger, IMemoryCache memoryCache) : BackgroundService
{
    private readonly Channel<Guid> _projectsToSync = Channel.CreateUnbounded<Guid>();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<SyncJobResult>> _projectsQueuedOrRunning = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var projectId in _projectsToSync.Reader.ReadAllAsync(stoppingToken))
        {
            using var activity = FwHeadlessActivitySource.Value.StartActivity("SyncHostedService.ExecuteAsync");
            await using var scope = services.CreateAsyncScope();
            var syncWorker = ActivatorUtilities.CreateInstance<SyncWorker>(scope.ServiceProvider, projectId);
            SyncJobResult result;
            try
            {
                result = await syncWorker.ExecuteSync(stoppingToken);
                logger.LogInformation("Sync job result: {Result}", result);
            }
            catch (Exception e)
            {
                activity?.AddException(e);
                logger.LogError(e, "Sync job failed");
                result = new SyncJobResult(SyncJobResultEnum.UnknownError, e.Message);
            }
            // Give clients a bit more time to poll the status
            CacheRecentSyncResult(projectId, result);
            _projectsQueuedOrRunning.TryRemove(projectId, out var tcs);
            tcs?.TrySetResult(result);
        }
    }

    public bool IsJobQueuedOrRunning(Guid projectId)
    {
        return _projectsQueuedOrRunning.ContainsKey(projectId);
    }

    public async Task<SyncJobResult?> AwaitSyncFinished(Guid projectId, CancellationToken cancellationToken)
    {
        if (_projectsQueuedOrRunning.TryGetValue(projectId, out var tcs))
            return await tcs.Task.WaitAsync(cancellationToken);
        return TryGetRecentSyncResult(projectId);
    }

    public bool QueueJob(Guid projectId)
    {
        //will only queue job if it's not already queued
        var addedToQueue = _projectsQueuedOrRunning.TryAdd(projectId, new());
        if (addedToQueue)
        {
            if (!_projectsToSync.Writer.TryWrite(projectId))
            {
                logger.LogError("Failed to queue sync job for project {ProjectId}, the channel is full", projectId);
                _projectsQueuedOrRunning.TryRemove(projectId, out _);
                return false;
            }

            logger.LogInformation("Queued sync job for project {ProjectId}", projectId);
        }
        else
        {
            logger.LogInformation("Project {ProjectId} is already queued", projectId);
        }

        return addedToQueue;
    }

    private void CacheRecentSyncResult(Guid projectId, SyncJobResult result)
    {
        memoryCache.Set($"SyncResult|{projectId}", result, TimeSpan.FromSeconds(30));
    }

    private SyncJobResult? TryGetRecentSyncResult(Guid projectId)
    {
        return memoryCache.Get<SyncJobResult>($"SyncResult|{projectId}");
    }
}

public class SyncWorker(
    Guid projectId,
    ILogger<SyncWorker> logger,
    IServiceProvider services,
    SendReceiveService srService,
    IOptions<FwHeadlessConfig> config,
    FwDataFactory fwDataFactory,
    CrdtProjectsService projectsService,
    ProjectLookupService projectLookupService,
    SyncJobStatusService syncStatusService,
    CrdtFwdataProjectSyncService syncService,
    CrdtHttpSyncService crdtHttpSyncService,
    IHttpClientFactory httpClientFactory
)
{
    public async Task<SyncJobResult> ExecuteSync(CancellationToken stoppingToken)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);
        logger.LogInformation("About to execute sync request for {projectId}", projectId);

        syncStatusService.StartSyncing(projectId);
        using var stopSyncing = Defer.Action(() => syncStatusService.StopSyncing(projectId));

        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null)
        {
            logger.LogError("Project ID {projectId} not found", projectId);
            activity?.SetStatus(ActivityStatusCode.Error, "Project not found");
            return new SyncJobResult(SyncJobResultEnum.ProjectNotFound, $"Project {projectId} not found");
        }

        activity?.SetTag("app.project_code", projectCode);

        logger.LogInformation("Project code is {projectCode}", projectCode);
        //if we can't sync with lexbox fail fast
        if (!await crdtHttpSyncService.TestAuth(httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName)))
        {
            logger.LogError("Unable to authenticate with Lexbox");
            activity?.SetStatus(ActivityStatusCode.Error, "Unable to authenticate with Lexbox");
            return new SyncJobResult(SyncJobResultEnum.UnableToAuthenticate, "Unable to authenticate with Lexbox");
        }

        var projectFolder = config.Value.GetProjectFolder(projectCode, projectId);
        if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);

        var crdtFile = config.Value.GetCrdtFile(projectCode, projectId);
        var fwDataProject = config.Value.GetFwDataProject(projectCode, projectId);
        logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
        logger.LogDebug("fwDataFile: {fwDataFile}", fwDataProject.FilePath);

        var fwdataApi = await SetupFwData(fwDataProject, projectCode);
        using var deferCloseFwData = fwDataFactory.DeferClose(fwDataProject);
        var crdtProject = await SetupCrdtProject(crdtFile, projectFolder, fwdataApi.ProjectId, fwDataProject);

        var miniLcmApi = await services.OpenCrdtProject(crdtProject);
        var crdtSyncService = services.GetRequiredService<CrdtSyncService>();
        await crdtSyncService.SyncHarmonyProject();

        var result = await syncService.Sync(miniLcmApi, fwdataApi);
        logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}",
            result.CrdtChanges,
            result.FwdataChanges);

        await crdtSyncService.SyncHarmonyProject();
        if (result.FwdataChanges == 0)
        {
            logger.LogInformation("No Send/Receive needed after CRDT sync as no FW changes were made by the sync");
        }
        else
        {
            var srResult2 = await srService.SendReceive(fwDataProject, projectCode);
            logger.LogInformation("Send/Receive result after CRDT sync: {srResult2}", srResult2.Output);
        }
        activity?.SetStatus(ActivityStatusCode.Ok, "Sync finished");
        return new SyncJobResult(SyncJobResultEnum.Success, null, result);
    }

    private async Task<FwDataMiniLcmApi> SetupFwData(FwDataProject fwDataProject, string projectCode)
    {
        if (File.Exists(fwDataProject.FilePath))
        {
            var pendingHgCommits = await srService.PendingCommitCount(fwDataProject, projectCode);
            if (pendingHgCommits == 0)
            {
                logger.LogInformation("No Send/Receive needed before CRDT sync as there are no pending commits");
            }
            else
            {
                var srResult = await srService.SendReceive(fwDataProject, projectCode);
                logger.LogInformation("Send/Receive result before CRDT sync: {srResult}", srResult.Output);
            }
        }
        else
        {
            var srResult = await srService.Clone(fwDataProject, projectCode);
            logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
        }

        var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, true);
        return fwdataApi;
    }

    private async Task<CrdtProject> SetupCrdtProject(string crdtFile,
        string projectFolder,
        Guid fwProjectId,
        FwDataProject fwDataProject)
    {
        var dbExists = File.Exists(crdtFile);
        if (CrdtFwdataProjectSyncService.IsSnapshotAvailable(fwDataProject) && dbExists)
        {
            return new CrdtProject("crdt", crdtFile);
        }

        if (await projectLookupService.IsCrdtProject(projectId))
        {
            //todo determine what to do in this case, maybe we just download the project?
            throw new InvalidOperationException("Project already exists, not sure why it's not on the server");
        }

        if (dbExists)
        {
            logger.LogWarning(
                "previous sync failed, indicated by a snapshot not existing, deleting the CRDT file: {crdtFile}",
                crdtFile);
            File.Delete(crdtFile);
        }

        return await projectsService.CreateProject(new("crdt",
            "crdt",
            SeedNewProjectData: false,
            Id: projectId,
            Path: projectFolder,
            FwProjectId: fwProjectId,
            Role: UserProjectRole.Editor,
            Domain: new Uri(config.Value.LexboxUrl)));
    }
}
