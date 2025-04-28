using System.Diagnostics;
using FwHeadless;
using FwHeadless.Services;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LexCore.Sync;
using LexData;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SIL.Harmony.Core;
using SIL.Harmony;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Trace;
using WebServiceDefaults;
using AppVersion = LexCore.AppVersion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddLexData(
    autoApplyMigrations: false,
    useOpenIddict: false,
    useSeeding: false
);

builder.Services.AddFwHeadless();
builder.AddServiceDefaults(AppVersion.Get(typeof(Program))).ConfigureAdditionalOpenTelemetry(telemetryBuilder =>
{
    telemetryBuilder.WithTracing(b => b.AddNpgsql()
        .AddEntityFrameworkCoreInstrumentation(c =>
        {
            //never emit traces for sqlite as there's way too much noise and it'll crash servers and overrun honeycomb
            c.Filter = (provider, command) => provider is not "Microsoft.EntityFrameworkCore.Sqlite";
            c.SetDbStatementForText = true;
        })
        .AddSource(FwHeadlessActivitySource.ActivitySourceName,
            FwLiteProjectSyncActivitySource.ActivitySourceName,
            FwDataMiniLcmBridgeActivitySource.ActivitySourceName,
            LcmCrdtActivitySource.ActivitySourceName));
});

var app = builder.Build();

// Add lexbox-version header to all requests
app.Logger.LogInformation("FwHeadless version: {version}", AppVersionService.Version);
app.Use(async (context, next) =>
{
    context.Response.Headers["lexbox-version"] = AppVersionService.Version;
    await next();
});

// Load project ID from request
app.Use((context, next) =>
{
    var renameThisService = context.RequestServices.GetRequiredService<ProjectContextFromIdService>();
    return renameThisService.PopulateProjectContext(context, next);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    //access at /scalar/v1
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

app.MapPost("/api/crdt-sync", ExecuteMergeRequest);
app.MapGet("/api/crdt-sync-status", GetMergeStatus);
app.MapGet("/api/await-sync-finished", AwaitSyncFinished);

// DELETE endpoint to remove a project if it exists
app.MapDelete("/api/manage/repo/{projectId}", async (Guid projectId,
    ProjectLookupService projectLookupService,
    IOptions<FwHeadlessConfig> config,
    SyncJobStatusService syncJobStatusService,
    ILogger<Program> logger) =>
{
    if (syncJobStatusService.SyncStatus(projectId) is SyncJobStatus.Running)
    {
        return Results.Conflict(new {message = "Sync job is running"});
    }
    var projectCode = await projectLookupService.GetProjectCode(projectId);
    if (projectCode is null)
    {
        logger.LogInformation("DELETE repo request for non-existent project {ProjectId}", projectId);
        return Results.NotFound(new { message = "Project not found" });
    }
    // Delete associated project folder if it exists
    var fwDataProject = config.Value.GetFwDataProject(projectCode, projectId);
    if (Directory.Exists(fwDataProject.ProjectFolder))
    {
        logger.LogInformation("Deleting repository for project {ProjectCode} ({ProjectId})", projectCode, projectId);
        Directory.Delete(fwDataProject.ProjectFolder, true);
    }
    else
    {
        logger.LogInformation("Repository for project {ProjectCode} ({ProjectId}) does not exist", projectCode, projectId);
    }
    return Results.Ok(new { message = "Repo deleted" });
});

app.Run();

static async Task<Results<Ok, NotFound, ProblemHttpResult>> ExecuteMergeRequest(
    SyncHostedService syncHostedService,
    ProjectLookupService projectLookupService,
    ILogger<Program> logger,
    CrdtHttpSyncService crdtHttpSyncService,
    IHttpClientFactory httpClientFactory,
    Guid projectId)
{
    var projectCode = await projectLookupService.GetProjectCode(projectId);
    if (projectCode is null)
    {
        logger.LogError("Project ID {projectId} not found", projectId);
        return TypedResults.NotFound();
    }

    logger.LogInformation("Project code is {projectCode}", projectCode);
    //if we can't sync with lexbox fail fast
    if (!await crdtHttpSyncService.TestAuth(httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName)))
    {
        logger.LogError("Unable to authenticate with Lexbox");
        return TypedResults.Problem("Unable to authenticate with Lexbox");
    }
    syncHostedService.QueueJob(projectId);
    return TypedResults.Ok();
}

static async Task<Results<Ok<ProjectSyncStatus>, NotFound>> GetMergeStatus(
    CurrentProjectService projectContext,
    ProjectLookupService projectLookupService,
    SendReceiveService srService,
    IOptions<FwHeadlessConfig> config,
    SyncJobStatusService syncJobStatusService,
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
    var lcmCrdtDbContext = services.GetRequiredService<LcmCrdtDbContext>();
    var localCrdtCommits = await lcmCrdtDbContext.Set<Commit>().CountAsync();
    var pendingCrdtCommits = crdtCommitsOnServer - localCrdtCommits;

    var lastCrdtCommitDate = await lcmCrdtDbContext.Set<Commit>().MaxAsync(commit => commit.HybridDateTime.DateTime);
    var lastHgCommitDate = lexboxProject.LastCommit;

    return TypedResults.Ok(ProjectSyncStatus.ReadyToSync(pendingCrdtCommits, await pendingHgCommits, lastCrdtCommitDate, lastHgCommitDate));
}

static async Task<Results<Ok<SyncJobResult>, NotFound, StatusCodeHttpResult>> AwaitSyncFinished(
    SyncHostedService syncHostedService,
    SyncJobStatusService syncJobStatusService,
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
            return TypedResults.NotFound();
        }

        activity?.SetStatus(ActivityStatusCode.Ok, "Sync finished");
        return TypedResults.Ok(result);
    }
    catch (OperationCanceledException)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Sync job timed out");
        return TypedResults.StatusCode(StatusCodes.Status408RequestTimeout);
    }
    catch (Exception e)
    {
        activity?.AddException(e);
        throw;
    }
}
