using FwHeadless;
using FwHeadless.Services;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LexCore.Sync;
using LexData;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using MiniLcm;
using Scalar.AspNetCore;
using LexCore.Utils;
using LinqToDB;
using SIL.Harmony.Core;
using SIL.Harmony;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddLexData(
    autoApplyMigrations: false,
    useOpenIddict: false
);

builder.Services.AddFwHeadless();

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

app.MapHealthChecks("/api/healthz");

app.MapPost("/api/crdt-sync", ExecuteMergeRequest);
app.MapGet("/api/crdt-sync-status", GetMergeStatus);
app.MapGet("/api/await-sync-finished", AwaitSyncFinished);

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
    SyncJobStatusService syncJobStatusService,
    IServiceProvider services,
    LexBoxDbContext lexBoxDb,
    SyncHostedService syncHostedService,
    Guid projectId)
{
    var jobStatus = syncJobStatusService.SyncStatus(projectId);
    if (jobStatus == SyncJobStatus.Running) return TypedResults.Ok(ProjectSyncStatus.Syncing);
    if (syncHostedService.IsJobQueuedOrRunning(projectId)) return TypedResults.Ok(ProjectSyncStatus.QueuedToSync);
    var project = projectContext.MaybeProject;
    if (project is null)
    {
        // 404 only means "project doesn't exist"; if we don't know the status, then it hasn't synced before and is therefore ready to sync
        if (await projectLookupService.ProjectExists(projectId)) return TypedResults.Ok(ProjectSyncStatus.NeverSynced);
        else return TypedResults.NotFound();
    }
    var commitsOnServer = await lexBoxDb.Set<ServerCommit>().CountAsync(c => c.ProjectId == projectId);
    var lcmCrdtDbContext = services.GetRequiredService<LcmCrdtDbContext>();
    var localCommits = await lcmCrdtDbContext.Set<Commit>().CountAsync();
    return TypedResults.Ok(ProjectSyncStatus.ReadyToSync(commitsOnServer - localCommits));
}

static async Task<Results<Ok<SyncJobResult>, NotFound, StatusCodeHttpResult>> AwaitSyncFinished(
    SyncHostedService syncHostedService,
    SyncJobStatusService syncJobStatusService,
    CancellationToken cancellationToken,
    Guid projectId)
{
    if (!syncHostedService.IsJobQueuedOrRunning(projectId)) return TypedResults.NotFound();
    try
    {

        var result = await syncHostedService.AwaitSyncFinished(projectId, cancellationToken);
        if (result is null) return TypedResults.NotFound();
        return TypedResults.Ok(result);
    }
    catch (OperationCanceledException e)
    {
        return TypedResults.StatusCode(StatusCodes.Status408RequestTimeout);
    }
}
