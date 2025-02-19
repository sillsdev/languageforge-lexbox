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
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

public class SyncHostedService(IServiceProvider services, ILogger<SyncHostedService> logger) : BackgroundService
{
    private readonly Channel<Guid> _projectsToSync = Channel.CreateUnbounded<Guid>();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<SyncJobResult>> _projectsQueuedOrRunning = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var projectId in _projectsToSync.Reader.ReadAllAsync(stoppingToken))
        {
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
                logger.LogError(e, "Sync job failed");
                result = new SyncJobResult(SyncJobResultEnum.UnknownError, e.Message);
            }
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

        return null;
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

        var projectFolder = Path.Join(config.Value.ProjectStorageRoot, $"{projectCode}-{projectId}");
        if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);

        var crdtFile = Path.Join(projectFolder, "crdt.sqlite");

        var fwDataProject = new FwDataProject("fw", projectFolder);
        logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
        logger.LogDebug("fwDataFile: {fwDataFile}", fwDataProject.FilePath);

        var fwdataApi = await SetupFwData(fwDataProject, srService, projectCode, logger, fwDataFactory);
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
        await crdtSyncService.SyncHarmonyProject();

        var result = await syncService.Sync(miniLcmApi, fwdataApi);
        logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}",
            result.CrdtChanges,
            result.FwdataChanges);

        await crdtSyncService.SyncHarmonyProject();
        var srResult2 = await srService.SendReceive(fwDataProject, projectCode);
        logger.LogInformation("Send/Receive result after CRDT sync: {srResult2}", srResult2.Output);
        activity?.SetStatus(ActivityStatusCode.Ok, "Sync finished");
        return new SyncJobResult(SyncJobResultEnum.Success, null, result);
    }

    static async Task<FwDataMiniLcmApi> SetupFwData(FwDataProject fwDataProject,
        SendReceiveService srService,
        string projectCode,
        ILogger logger,
        FwDataFactory fwDataFactory)
    {
        if (File.Exists(fwDataProject.FilePath))
        {
            var srResult = await srService.SendReceive(fwDataProject, projectCode);
            logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
        }
        else
        {
            var srResult = await srService.Clone(fwDataProject, projectCode);
            logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
        }

        var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, true);
        return fwdataApi;
    }

    static async Task<CrdtProject> SetupCrdtProject(string crdtFile,
        ProjectLookupService projectLookupService,
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
                SeedNewProjectData: false,
                Id: projectId,
                Path: projectFolder,
                FwProjectId: fwProjectId,
                Domain: new Uri(lexboxUrl)));
        }

    }
}
