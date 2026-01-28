using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwHeadless.Media;
using FwLiteProjectSync;
using LcmCrdt;
using LcmCrdt.MediaServer;
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
                result = new SyncJobResult(SyncJobStatusEnum.UnknownError, e.ToString());
            }

            if (result.Error is not null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"Sync job failed: {result.Error}");
            }

            // Give clients a bit more time to poll the status
            CacheRecentSyncResult(projectId, result);
            _projectsQueuedOrRunning.TryRemove(projectId, out var tcs);
            tcs?.TrySetResult(result);
        }
    }

    public virtual bool IsJobQueuedOrRunning(Guid projectId)
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
    ISendReceiveService srService,
    IOptions<FwHeadlessConfig> config,
    FwDataFactory fwDataFactory,
    CrdtProjectsService projectsService,
    IProjectLookupService projectLookupService,
    ISyncJobStatusService syncStatusService,
    CrdtFwdataProjectSyncService syncService,
    CrdtHttpSyncService crdtHttpSyncService,
    IHttpClientFactory httpClientFactory,
    MediaFileService mediaFileService,
    IProjectMetadataService metadataService
)
{
    public async Task<SyncJobResult> ExecuteSync(CancellationToken stoppingToken, bool onlyHarmony = false)
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
            return new SyncJobResult(SyncJobStatusEnum.ProjectNotFound, $"Project {projectId} not found");
        }

        // Check if project is blocked (defensive check in case it was blocked while waiting in queue)
        var blockInfo = await metadataService.GetSyncBlockInfoAsync(projectId);
        if (blockInfo?.IsBlocked == true)
        {
            logger.LogInformation("Project {projectId} is blocked from syncing. Reason: {Reason}", projectId, blockInfo.Reason);
            activity?.SetStatus(ActivityStatusCode.Ok, $"Project blocked from sync: {blockInfo.Reason}");
            return new SyncJobResult(SyncJobStatusEnum.SyncBlocked, $"Project is blocked from syncing. Reason: {blockInfo.Reason}");
        }

        activity?.SetTag("app.project_code", projectCode);

        logger.LogInformation("Project code is {projectCode}", projectCode);
        //if we can't sync with lexbox fail fast
        if (!await crdtHttpSyncService.TestAuth(httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName)))
        {
            logger.LogError("Unable to authenticate with Lexbox");
            activity?.SetStatus(ActivityStatusCode.Error, "Unable to authenticate with Lexbox");
            return new SyncJobResult(SyncJobStatusEnum.UnableToAuthenticate, "Unable to authenticate with Lexbox");
        }

        var projectFolder = config.Value.GetProjectFolder(projectCode, projectId);
        if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);

        var crdtFile = config.Value.GetCrdtFile(projectCode, projectId);
        var fwDataProject = config.Value.GetFwDataProject(projectCode, projectId);
        logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
        logger.LogDebug("fwDataFile: {fwDataFile}", fwDataProject.FilePath);

        FwDataMiniLcmApi? fwdataApi;
        try
        {
            fwdataApi = await SetupFwData(fwDataProject, projectCode);
        }
        catch (SendReceiveException e)
        {
            // Detect rollback via the "Rolling back..." message from Chorus:
            // https://github.com/sillsdev/chorus/blob/master/src/LibChorus/sync/Synchronizer.cs#L651
            if (e.Message.Contains("Rolling back..."))
            {
                await metadataService.BlockFromSyncAsync(projectId, "Rollback detected during Send/Receive");
                return new SyncJobResult(SyncJobStatusEnum.SyncBlocked, "Project blocked due to rollback");
            }
            activity?.SetStatus(ActivityStatusCode.Error, "Send/Receive failed before CRDT sync");
            return new SyncJobResult(SyncJobStatusEnum.SendReceiveFailed, e.Message);
        }
        //always do this as existing projects need to run this even if they didn't S&R due to no pending changes
        await mediaFileService.SyncMediaFiles(fwdataApi.Cache);

        using var deferCloseFwData = fwDataFactory.DeferClose(fwDataProject);
        var crdtProject = await SetupCrdtProject(crdtFile,
            projectLookupService,
            projectId,
            projectsService,
            projectFolder,
            fwdataApi.ProjectId,
            config.Value.LexboxUrl);

        var miniLcmApi = await services.OpenCrdtProject(crdtProject);
        var crdtSyncService = services.GetRequiredService<CrdtSyncService>();

        // If the last merge was successful, we can sync the Harmony project, otherwise we risk pushing a partial sync
        if (CrdtFwdataProjectSyncService.HasSyncedSuccessfully(fwDataProject) || onlyHarmony)
        {
            await crdtSyncService.SyncHarmonyProject();
        }
        await mediaFileService.SyncMediaFiles(projectId, services.GetRequiredService<LcmMediaService>());

        if (onlyHarmony)
        {
            // Getting this far allows us to restore a reset project, so we can regenerate a snapshot from it
            activity?.SetStatus(ActivityStatusCode.Ok, "Only Harmony sync requested, skipping Mercurial/Crdt sync");
            return new SyncJobResult(SyncJobStatusEnum.Success, "Only Harmony sync requested, skipping Mercurial/Crdt sync");
        }

        var projectSnapshot = await syncService.GetProjectSnapshot(fwdataApi.Project);
        var result = await syncService.Sync(miniLcmApi, fwdataApi, projectSnapshot);
        logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}",
            result.CrdtChanges,
            result.FwdataChanges);

        if (result.FwdataChanges == 0)
        {
            logger.LogInformation("No Send/Receive needed after CRDT sync as no FW changes were made by the sync");
        }
        else
        {
            var srResult2 = await srService.SendReceive(fwDataProject, projectCode);
            if (!srResult2.Success)
            {
                // Detect rollback via the "Rolling back..." message from Chorus:
                // https://github.com/sillsdev/chorus/blob/main/src/LibChorus/sync/Synchronizer.cs#L651
                if (srResult2.Output.Contains("Rolling back..."))
                {
                    await metadataService.BlockFromSyncAsync(projectId, "Rollback detected during Send/Receive");
                    return new SyncJobResult(SyncJobStatusEnum.SyncBlocked, "Project blocked due to rollback");
                }
                logger.LogError("Send/Receive after CRDT sync failed: {Output}", srResult2.Output);
                activity?.SetStatus(ActivityStatusCode.Error, "Send/Receive failed after CRDT sync");
                return new SyncJobResult(SyncJobStatusEnum.SendReceiveFailed, $"Send/Receive after CRDT sync failed: {srResult2.Output}");
            }
            else
            {
                logger.LogInformation("Send/Receive result after CRDT sync: {Output}", srResult2.Output);
            }
        }

        //We only want to regenerate the snapshot if we know the changes applied to fwdata were successfully pushed.
        //Otherwise, the changes might have been rolled back.
        if (result.CrdtChanges > 0)
        {
            logger.LogInformation("Regenerating project snapshot to include crdt changes");
            //note we are now using the crdt API, this avoids issues where some data isn't synced yet
            //later when we add the ability to sync that data we need the snapshot to reflect the synced state, not what was in the FW project
            //related to https://github.com/sillsdev/languageforge-lexbox/issues/1912
            await syncService.RegenerateProjectSnapshot(miniLcmApi, fwdataApi.Project, keepBackup: false);
        }
        else
        {
            logger.LogInformation("Skipping regenerating project snapshot, because there were no crdt changes");
        }

        await crdtSyncService.SyncHarmonyProject();

        activity?.SetStatus(ActivityStatusCode.Ok, "Sync finished");
        return new SyncJobResult(result);
    }

    protected virtual async Task<FwDataMiniLcmApi> SetupFwData(FwDataProject fwDataProject, string projectCode)
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
                if (!srResult.Success)
                {
                    logger.LogError("Send/Receive before CRDT sync failed: {Output}", srResult.Output);
                    throw new SendReceiveException($"Send/Receive before CRDT sync failed: {srResult.Output}");
                }
                else
                {
                    logger.LogInformation("Send/Receive result before CRDT sync: {Output}", srResult.Output);
                }

            }
        }
        else
        {
            var srResult = await srService.Clone(fwDataProject, projectCode);
            if (!srResult.Success)
            {
                logger.LogError("Clone before CRDT sync failed: {Output}", srResult.Output);
                throw new SendReceiveException($"Clone before CRDT sync failed: {srResult.Output}");
            }
            else
            {
                logger.LogInformation("Clone result before CRDT sync: {Output}", srResult.Output);
            }
        }

        var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, true);
        return fwdataApi;
    }

    protected virtual async Task<CrdtProject> SetupCrdtProject(string crdtFile,
        IProjectLookupService projectLookupService,
        Guid projectId,
        CrdtProjectsService projectsService,
        string projectFolder,
        Guid fwProjectId,
        string lexboxUrl)
    {
        if (File.Exists(crdtFile))
        {
            return new CrdtProject("crdt", crdtFile);
        }
        else
        {
            if (await projectLookupService.IsCrdtProject(projectId))
            {
                //todo determine what to do in this case, maybe we just download the project?
                throw new InvalidOperationException("Project already exists, not sure why it's not on the server");
            }

            return await projectsService.CreateProject(new("crdt",
                "crdt",
                SeedNewProjectData: false,
                Id: projectId,
                Path: projectFolder,
                FwProjectId: fwProjectId,
                Role: UserProjectRole.Editor,
                Domain: new Uri(lexboxUrl)));
        }

    }
}
