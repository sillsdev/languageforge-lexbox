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

    // Projects currently being created and populated with an initial .fwdata file. Tracked here (not just in ProjectCreationService)
    // so a sync job can't be queued for a project mid-creation and race on the same fw/ folder and repo.
    // The TaskCompletionSource carries the outcome to anyone long-polling AwaitCreationFinished.
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ProjectCreationStatus>> _projectsBeingCreated = new();

    // Serialises the check-then-add across the two dictionaries above, so a creation and a sync can't
    // both slip through by interleaving their checks.
    // This requires that SyncHostedService be a singleton, of course
    private readonly Lock _reservationLock = new();

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
        return _projectsQueuedOrRunning.ContainsKey(projectId) || _projectsBeingCreated.ContainsKey(projectId);
    }

    /// <summary>
    /// Reserves a project for creation-and-population, blocking a concurrent creation or sync of the
    /// same project. Returns false if a sync is already queued/running or another creation is in flight.
    /// Pair with <see cref="EndProjectCreation"/> in a finally block.
    /// </summary>
    public bool TryStartProjectCreation(Guid projectId)
    {
        lock (_reservationLock)
        {
            if (_projectsQueuedOrRunning.ContainsKey(projectId)) return false;
            // RunContinuationsAsynchronously so a long-polling awaiter's continuation can't run inline on
            // the thread that finished the creation.
            return _projectsBeingCreated.TryAdd(projectId,
                new TaskCompletionSource<ProjectCreationStatus>(TaskCreationOptions.RunContinuationsAsynchronously));
        }
    }

    /// <summary>
    /// Releases the reservation and publishes the outcome to pollers. Must be called on every exit path
    /// from a creation, or an AwaitCreationFinished caller waits until its own token trips.
    /// </summary>
    public void EndProjectCreation(Guid projectId, ProjectCreationStatus result)
    {
        // Cache before removing the reservation, so a poller that sees "not in flight" always finds the
        // result waiting for it rather than a gap.
        CacheRecentCreationResult(projectId, result);
        _projectsBeingCreated.TryRemove(projectId, out var tcs);
        tcs?.TrySetResult(result);
    }

    public bool IsProjectBeingCreated(Guid projectId) => _projectsBeingCreated.ContainsKey(projectId);

    public async Task<SyncJobResult?> AwaitSyncFinished(Guid projectId, CancellationToken cancellationToken)
    {
        if (_projectsQueuedOrRunning.TryGetValue(projectId, out var tcs))
            return await tcs.Task.WaitAsync(cancellationToken);
        return TryGetRecentSyncResult(projectId);
    }

    /// <summary>
    /// Waits for an in-flight creation to finish. Returns the recent result if one just finished, or
    /// null when this FwHeadless has no record of a creation for the project (the caller should then
    /// derive the status from durable state instead).
    /// </summary>
    public async Task<ProjectCreationStatus?> AwaitCreationFinished(Guid projectId, CancellationToken cancellationToken)
    {
        if (_projectsBeingCreated.TryGetValue(projectId, out var tcs))
            return await tcs.Task.WaitAsync(cancellationToken);
        return TryGetRecentCreationResult(projectId);
    }

    public ProjectCreationStatus? TryGetRecentCreationResult(Guid projectId)
    {
        return memoryCache.Get<ProjectCreationStatus>($"CreationResult|{projectId}");
    }

    private void CacheRecentCreationResult(Guid projectId, ProjectCreationStatus result)
    {
        // Longer than the sync equivalent's 30s: a caller whose own request timed out partway through a
        // creation should still be able to come back and learn how it ended.
        memoryCache.Set($"CreationResult|{projectId}", result, TimeSpan.FromMinutes(5));
    }

    public bool QueueJob(Guid projectId)
    {
        bool addedToQueue;
        lock (_reservationLock)
        {
            //don't sync a project while it's still being created and populated with initial .fwdata (would race on the repo)
            if (_projectsBeingCreated.ContainsKey(projectId))
            {
                logger.LogInformation("Project {ProjectId} is being created, not queueing a sync job", projectId);
                return false;
            }
            //will only queue job if it's not already queued
            addedToQueue = _projectsQueuedOrRunning.TryAdd(projectId, new());
        }
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
    ProjectSnapshotService projectSnapshotService,
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

        activity?.SetTag("app.project_code", projectCode);

        logger.LogInformation("Project code is {projectCode}", projectCode);
        //if we can't sync with lexbox fail fast
        if (!await crdtHttpSyncService.TestAuth(httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName)))
        {
            logger.LogError("Unable to authenticate with Lexbox");
            activity?.SetStatus(ActivityStatusCode.Error, "Unable to authenticate with Lexbox");
            return new SyncJobResult(SyncJobStatusEnum.UnableToAuthenticate, "Unable to authenticate with Lexbox");
        }

        // Check if project is blocked (defensive check in case it was blocked while waiting in queue)
        var blockInfo = await metadataService.GetSyncBlockedInfoAsync(projectId);
        if (blockInfo?.IsBlocked == true)
        {
            logger.LogInformation("Project {projectId} is blocked from syncing. Reason: {Reason}", projectId, blockInfo.Reason);
            activity?.SetStatus(ActivityStatusCode.Ok, $"Project blocked from sync: {blockInfo.Reason}");
            return new SyncJobResult(SyncJobStatusEnum.SyncBlocked, $"Project is blocked from syncing. Reason: {blockInfo.Reason}");
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
            if (e.Result.RollbackDetected)
            {
                await metadataService.BlockFromSyncAsync(projectId, "Rollback detected during Send/Receive");
                return new SyncJobResult(SyncJobStatusEnum.SyncBlocked, "Project blocked due to rollback");
            }
            activity?.SetStatus(ActivityStatusCode.Error, "Send/Receive failed before CRDT sync");
            return new SyncJobResult(SyncJobStatusEnum.SendReceiveFailed, e.Message);
        }
        catch (InvalidFwDataProjectException e)
        {
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            return new SyncJobResult(SyncJobStatusEnum.ProjectIncompatible, e.Message);
        }
        //always do this as existing projects need to run this even if they didn't S&R due to no pending changes
        await mediaFileService.SyncMediaFiles(fwdataApi.Cache);

        await using var deferCloseFwData = fwDataFactory.DeferCloseAsync(fwDataProject);
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
        if (ProjectSnapshotService.HasSyncedSuccessfully(fwDataProject) || onlyHarmony)
        {
            await crdtSyncService.SyncHarmonyProject();
        }
        await mediaFileService.SyncMediaFiles(projectId, services.GetRequiredService<LcmMediaService>());

        if (onlyHarmony)
        {
            // Getting this far allows us to restore a reset project, so we can regenerate a snapshot from it
            activity?.SetStatus(ActivityStatusCode.Ok, "Only Harmony sync requested, skipping Mercurial/Crdt sync");
            return new SyncJobResult(SyncJobStatusEnum.SuccessHarmonyOnly, "Only Harmony sync requested, skipping Mercurial/Crdt sync");
        }

        var projectSnapshot = await projectSnapshotService.GetProjectSnapshot(fwdataApi.Project);
        var result = projectSnapshot is null
            ? await syncService.Import(miniLcmApi, fwdataApi)
            : await syncService.Sync(miniLcmApi, fwdataApi, projectSnapshot);

        logger.LogInformation("{ImportOrSync} result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}",
            projectSnapshot is null ? "Import" : "Sync",
            result.CrdtChanges,
            result.FwdataChanges);

        if (result.FwdataChanges == 0)
        {
            logger.LogInformation("No Send/Receive needed after CRDT sync as no FW changes were made by the sync");
        }
        else
        {
            var srResult2 = await srService.SendReceive(fwDataProject, projectCode);
            // HTTP 500 errors should be retried once before checking for success
            if (srResult2.InternalServerError)
            {
                srResult2 = await srService.SendReceive(fwDataProject, projectCode);
            }
            if (!srResult2.Success)
            {
                if (srResult2.RollbackDetected)
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

        /*
        Notes:
        1) We defer regenerating the snapshot until we know the changes applied to fwdata were successfully pushed.
        Otherwise, the changes might have been rolled back. In that case, the next sync could overwrite CRDT data with old/rolled back FW data.
        2) We are intentionaly using the crdt API. This avoids issues when new data/fields don't yet get synced.
        When we start syncing that data/those fields we need the snapshot to reflect the CRDT state, rather than the FW project,
        otherwise existing FW data will never be synced to CRDT. Related to https://github.com/sillsdev/languageforge-lexbox/issues/1912
        3) We should ALWAYS regenerate the snapshot even if no crdt or fwdata changes were detected.
        If no crdt changes were detected we still might have pulled in crdt commits that were applied to fw.
        If none of the changes needed to be applied to fw (i.e. also no fw changes), those same changes were maybe/presumably already made in fw as well.
        If the same change was made in both fwdata and crdt, no changes would be detected, but we still need that change to get into the snapshot
        */
        logger.LogInformation("Regenerating project snapshot");
        await projectSnapshotService.RegenerateProjectSnapshot(miniLcmApi, fwdataApi.Project, keepBackup: false);

        /*
        Push new changes to Harmony (changes that came from FW)
        Important: we only sync the Harmony project AFTER regenerating the snapshot.
        Otherwise, the sync could pull changes into the snapshot that were not respected during the sync.
        Could presumably be skipped if 0 CrdtChanges, but it's cheap.
        */
        await crdtSyncService.SyncHarmonyProject();

        activity?.SetStatus(ActivityStatusCode.Ok, "Sync finished");
        return new SyncJobResult(result);
    }

    protected virtual async Task<FwDataMiniLcmApi> SetupFwData(FwDataProject fwDataProject, string projectCode)
    {
        if (File.Exists(fwDataProject.FilePath))
        {
            var pendingHgCommits = await srService.PendingCommitCountBothWays(fwDataProject, projectCode);
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
                    throw new SendReceiveException("Send/Receive before CRDT sync failed", srResult);
                }
                else
                {
                    logger.LogInformation("Send/Receive result before CRDT sync: {Output}", srResult.Output);
                }

            }
        }
        else
        {
            try
            {
                var srResult = await srService.Clone(fwDataProject, projectCode);
                if (!srResult.Success)
                {
                    logger.LogError("Clone before CRDT sync failed: {Output}", srResult.Output);
                    throw new SendReceiveException("Clone before CRDT sync failed", srResult);
                }

                logger.LogInformation("Clone result before CRDT sync: {Output}", srResult.Output);

                if (!File.Exists(fwDataProject.FilePath))
                {
                    var message = $"FieldWorks project file '{fwDataProject.FilePath}' was not found after Clone. " +
                                  "This likely means that the LexBox repository does not contain a FieldWorks project (e.g. it may be a WeSay project).";
                    logger.LogError(message);
                    throw new InvalidFwDataProjectException(message, fwDataProject.FilePath);
                }
            }
            catch
            {
                try
                {
                    if (Directory.Exists(fwDataProject.ProjectFolder))
                    {
                        logger.LogInformation("Cleaning up FW data folder after failed clone: {FwDataFolder}", fwDataProject.ProjectFolder);
                        Directory.Delete(fwDataProject.ProjectFolder, true);
                        logger.LogInformation("Removed FW data folder");
                        // fwDataProject.ProjectsPath is actually "{code}-{id}" i.e. the parent folder of this SINGLE project.
                        // will throw if not empty (e.g. crdt db or snapshot backup), which is good. It's an interesting edge case.
                        Directory.Delete(fwDataProject.ProjectsPath);
                        logger.LogInformation("Removed empty project folder: {ProjectFolder}", fwDataProject.ProjectsPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    logger.LogWarning(cleanupEx, "Failed to clean up project after failed clone");
                }
                throw;
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
                Id: projectId,
                Path: projectFolder,
                FwProjectId: fwProjectId,
                Role: UserProjectRole.Editor,
                Domain: new Uri(lexboxUrl)));
        }

    }
}
