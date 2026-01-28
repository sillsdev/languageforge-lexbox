using System.Diagnostics;
using FwHeadless.Services;
using FwLiteProjectSync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LexCore.Sync;
using LexData;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniLcm;
using SIL.Harmony;
using SIL.Harmony.Core;

namespace FwHeadless.Routes;

public static class MergeRoutes
{
    public static IEndpointConventionBuilder MapMergeRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/merge").WithOpenApi();

        group.MapPost("/execute", ExecuteMergeRequest);
        group.MapPost("/sync-harmony", SyncHarmonyProject);
        group.MapPost("/regenerate-snapshot", RegenerateProjectSnapshot);
        group.MapGet("/status", GetMergeStatus);
        group.MapGet("/await-finished", AwaitSyncFinished);
        group.MapPost("/block", BlockProject);
        group.MapPost("/unblock", UnblockProject);
        group.MapGet("/block-status", GetBlockStatus);
        return group;
    }


    static async Task<Results<Ok, ProblemHttpResult>> ExecuteMergeRequest(
        SyncHostedService syncHostedService,
        IProjectLookupService projectLookupService,
        IProjectMetadataService metadataService,
        ILogger<Program> logger,
        CrdtHttpSyncService crdtHttpSyncService,
        IHttpClientFactory httpClientFactory,
        Guid projectId)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);

        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null)
        {
            logger.LogError("Project ID {projectId} not found", projectId);
            activity?.SetStatus(ActivityStatusCode.Error, "Project not found");
            return TypedResults.Problem("Project not found", statusCode: StatusCodes.Status404NotFound);
        }

        logger.LogInformation("Project code is {projectCode}", projectCode);
        activity?.SetTag("app.project_code", projectCode);

        //if we can't sync with lexbox fail fast
        if (!await crdtHttpSyncService.TestAuth(httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName)))
        {
            logger.LogError("Unable to authenticate with Lexbox");
            activity?.SetStatus(ActivityStatusCode.Error, "Unable to authenticate with Lexbox");
            return TypedResults.Problem("Unable to authenticate with Lexbox", statusCode: StatusCodes.Status403Forbidden);
        }

        // Check if project is blocked from syncing
        var blockInfo = await metadataService.GetSyncBlockInfoAsync(projectId);
        if (blockInfo?.IsBlocked == true)
        {
            logger.LogInformation("Project {projectId} is blocked from syncing. Reason: {Reason}", projectId, blockInfo.Reason);
            activity?.SetStatus(ActivityStatusCode.Ok, $"Project blocked from sync: {blockInfo.Reason}");
            return TypedResults.Problem($"Project is blocked from syncing. Reason: {blockInfo.Reason}", statusCode: StatusCodes.Status423Locked);
        }

        syncHostedService.QueueJob(projectId);
        activity?.SetStatus(ActivityStatusCode.Ok, "Sync job queued");
        return TypedResults.Ok();
    }

    static async Task<Results<Ok, NotFound<string>>> SyncHarmonyProject(
        Guid projectId,
        IProjectLookupService projectLookupService,
        CrdtSyncService crdtSyncService,
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);

        if (!await projectLookupService.ProjectExists(projectId))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Project not found");
            return TypedResults.NotFound("Project not found");
        }

        var syncWorker = ActivatorUtilities.CreateInstance<SyncWorker>(services, projectId);
        await syncWorker.ExecuteSync(stoppingToken, onlyHarmony: true);

        return TypedResults.Ok();
    }


    static async Task<Results<Ok, NotFound<string>>> RegenerateProjectSnapshot(
        Guid projectId,
        CurrentProjectService projectContext,
        IProjectLookupService projectLookupService,
        CrdtFwdataProjectSyncService syncService,
        SnapshotAtCommitService snapshotAtCommitService,
        IOptions<FwHeadlessConfig> config,
        HttpContext context,
        Guid? commitId = null,
        bool preserveAllFieldWorksCommits = false
    )
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);
        var project = projectContext.MaybeProject;
        if (project is null)
        {
            // 404 only means "project doesn't exist"; if we don't know the status, then it hasn't synced before and is therefore ready to sync
            if (await projectLookupService.ProjectExists(projectId))
            {
                activity?.SetStatus(ActivityStatusCode.Unset, "Project never synced");
                return TypedResults.NotFound("Project never synced");
            }

            activity?.SetStatus(ActivityStatusCode.Error, "Project not found");
            return TypedResults.NotFound("Project not found");
        }

        var fwDataProject = config.Value.GetFwDataProject(projectId);
        if (commitId.HasValue)
        {
            if (!await syncService.RegenerateProjectSnapshotAtCommit(snapshotAtCommitService, fwDataProject, commitId.Value, preserveAllFieldWorksCommits))
            {
                return TypedResults.NotFound($"Commit {commitId} not found");
            }
        }
        else
        {
            var miniLcmApi = context.RequestServices.GetRequiredService<IMiniLcmApi>();
            await syncService.RegenerateProjectSnapshot(miniLcmApi, fwDataProject);
        }
        return TypedResults.Ok();
    }

    static async Task<Results<Ok<ProjectSyncStatus>, NotFound>> GetMergeStatus(
        CurrentProjectService projectContext,
        IProjectLookupService projectLookupService,
        ISendReceiveService srService,
        IOptions<FwHeadlessConfig> config,
        ISyncJobStatusService syncJobStatusService,
        IServiceProvider services,
        LexBoxDbContext lexBoxDb,
        SyncHostedService syncHostedService,
        Guid projectId)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);
        var jobStatus = syncJobStatusService.SyncStatus(projectId);
        if (jobStatus == SyncJobStatus.Running) return TypedResults.Ok(ProjectSyncStatus.Syncing);
        if (syncHostedService.IsJobQueuedOrRunning(projectId)) return TypedResults.Ok(ProjectSyncStatus.QueuedToSync);
        var project = projectContext.MaybeProject;
        if (project is null)
        {
            // 404 only means "project doesn't exist"; if we don't know the status, then it hasn't synced before and is therefore ready to sync
            if (await projectLookupService.ProjectExists(projectId))
            {
                activity?.SetStatus(ActivityStatusCode.Unset, "Project never synced");
                return TypedResults.Ok(ProjectSyncStatus.NeverSynced);
            }
            activity?.SetStatus(ActivityStatusCode.Error, "Project not found");
            return TypedResults.NotFound();
        }
        var lexboxProject = await lexBoxDb.Projects.Include(p => p.FlexProjectMetadata).FirstOrDefaultAsync(p => p.Id == projectId);
        if (lexboxProject is null)
        {
            // Can't sync if lexbox doesn't have this project
            activity?.SetStatus(ActivityStatusCode.Error, "Lexbox project not found");
            return TypedResults.NotFound();
        }
        activity?.SetTag("app.project_code", lexboxProject.Code);
        var projectFolder = config.Value.GetProjectFolder(lexboxProject.Code, projectId);
        if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);
        var fwDataProject = config.Value.GetFwDataProject(lexboxProject.Code, projectId);
        var pendingHgCommits = srService.PendingCommitCount(fwDataProject, lexboxProject.Code); // NOT awaited here so that this long-running task can run in parallel with others

        var crdtCommitsOnServer = await lexBoxDb.Set<ServerCommit>().CountAsync(c => c.ProjectId == projectId);
        await using var lcmCrdtDbContext = await services.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        var localCrdtCommits = await lcmCrdtDbContext.Set<Commit>().CountAsync();
        var pendingCrdtCommits = crdtCommitsOnServer - localCrdtCommits;

        var lastCrdtCommitDate = await lcmCrdtDbContext.Set<Commit>().MaxAsync(commit => commit.HybridDateTime.DateTime);
        var lastHgCommitDate = lexboxProject.LastCommit;

        return TypedResults.Ok(ProjectSyncStatus.ReadyToSync(pendingCrdtCommits, await pendingHgCommits, lastCrdtCommitDate, lastHgCommitDate));
    }

    static async Task<SyncJobResult> AwaitSyncFinished(
        SyncHostedService syncHostedService,
        ISyncJobStatusService syncJobStatusService,
        CancellationToken cancellationToken,
        Guid projectId)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        try
        {
            var result = await syncHostedService.AwaitSyncFinished(projectId, cancellationToken);
            if (result is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Sync job not found");
                return new(SyncJobStatusEnum.SyncJobNotFound, "Sync job not found");
            }

            activity?.SetStatus(ActivityStatusCode.Ok, "Sync finished");
            return result;
        }
        catch (OperationCanceledException e)
        {
            if (e.CancellationToken == cancellationToken)
            {
                // The AwaitSyncFinished call was canceled, but the sync job was not (necessarily) canceled
                activity?.SetStatus(ActivityStatusCode.Unset, "Timed out awaiting sync status");
                return new SyncJobResult(SyncJobStatusEnum.TimedOutAwaitingSyncStatus, "Timed out awaiting sync status");
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Sync job timed out");
                return new SyncJobResult(SyncJobStatusEnum.SyncJobTimedOut, "Sync job timed out");
            }
        }
        catch (Exception e)
        {
            activity?.AddException(e);
            var error = e.ToString();
            // TODO: Consider only returning exception error for certain users (admins, devs, managers)?
            // Note 200 OK returned here; getting the status is a successful HTTP request even if the status is "the job failed and here's why"
            return new SyncJobResult(SyncJobStatusEnum.CrdtSyncFailed, error);
        }
    }

    static async Task<Results<Ok, NotFound, BadRequest<string>>> BlockProject(
        IProjectLookupService projectLookupService,
        IProjectMetadataService metadataService,
        ILogger<Program> logger,
        Guid projectId,
        string? reason = null)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);

        if (!await projectLookupService.ProjectExists(projectId))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Project ID not found in database");
            logger.LogWarning("Project ID {projectId} not found in database", projectId);
            return TypedResults.NotFound();
        }

        try
        {
            await metadataService.BlockFromSyncAsync(projectId, reason);
            activity?.SetStatus(ActivityStatusCode.Ok, "Project blocked from sync");
            logger.LogInformation("Project {projectId} blocked from sync", projectId);
            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            logger.LogError(ex, "Error blocking project {projectId}", projectId);
            return TypedResults.BadRequest("Failed to block project");
        }
    }

    static async Task<Results<Ok, NotFound, BadRequest<string>>> UnblockProject(
        IProjectLookupService projectLookupService,
        IProjectMetadataService metadataService,
        ILogger<Program> logger,
        Guid projectId)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);

        if (!await projectLookupService.ProjectExists(projectId))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Project ID not found in database");
            logger.LogWarning("Project ID {projectId} not found in database", projectId);
            return TypedResults.NotFound();
        }

        try
        {
            await metadataService.UnblockFromSyncAsync(projectId);
            activity?.SetStatus(ActivityStatusCode.Ok, "Project unblocked from sync");
            logger.LogInformation("Project {projectId} unblocked from sync", projectId);
            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            logger.LogError(ex, "Error unblocking project {projectId}", projectId);
            return TypedResults.BadRequest("Failed to unblock project");
        }
    }

    static async Task<Results<Ok<SyncBlockStatus>, NotFound, BadRequest<string>>> GetBlockStatus(
        IProjectLookupService projectLookupService,
        IProjectMetadataService metadataService,
        ILogger<Program> logger,
        Guid projectId)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);

        if (!await projectLookupService.ProjectExists(projectId))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Project ID not found in database");
            logger.LogWarning("Project ID {projectId} not found in database", projectId);
            return TypedResults.NotFound();
        }

        var blockInfo = await metadataService.GetSyncBlockInfoAsync(projectId);

        activity?.SetStatus(ActivityStatusCode.Ok, $"Block status retrieved: {(blockInfo?.IsBlocked == true ? "blocked" : "unblocked")}");
        return TypedResults.Ok(new SyncBlockStatus
        {
            IsBlocked = blockInfo?.IsBlocked ?? false,
            Reason = blockInfo?.Reason,
            BlockedAt = blockInfo?.BlockedAt
        });
    }
}
