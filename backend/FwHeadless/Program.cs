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

app.Run();

static async Task<Results<Ok<SyncResult>, NotFound, ProblemHttpResult>> ExecuteMergeRequest(
    ILogger<Program> logger,
    IServiceProvider services,
    SendReceiveService srService,
    IOptions<FwHeadlessConfig> config,
    FwDataFactory fwDataFactory,
    CrdtProjectsService projectsService,
    ProjectLookupService projectLookupService,
    ProjectSyncStatusService syncStatusService,
    CrdtFwdataProjectSyncService syncService,
    CrdtHttpSyncService crdtHttpSyncService,
    IHttpClientFactory httpClientFactory,
    Guid projectId,
    bool dryRun = false)
{
    logger.LogInformation("About to execute sync request for {projectId}", projectId);
    if (dryRun)
    {
        logger.LogInformation("Dry run, not actually syncing");
        return TypedResults.Ok(new SyncResult(0, 0));
    }

    syncStatusService.StartSyncing(projectId);
    using var stopSyncing = Defer.Action(() => syncStatusService.StopSyncing(projectId));

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
        return TypedResults.Problem("Unable to authenticate with Lexbox");
    }

    var projectFolder = Path.Join(config.Value.ProjectStorageRoot, $"{projectCode}-{projectId}");
    if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);

    var crdtFile = Path.Join(projectFolder, "crdt.sqlite");

    var fwDataProject = new FwDataProject("fw", projectFolder);
    logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
    logger.LogDebug("fwDataFile: {fwDataFile}", fwDataProject.FilePath);

    var fwdataApi = await SetupFwData(fwDataProject, srService, projectCode, logger, fwDataFactory);
    using var deferCloseFwData = fwDataFactory.DeferClose(fwDataProject);
    var crdtProject = await SetupCrdtProject(crdtFile, projectLookupService, projectId, projectsService, projectFolder, fwdataApi.ProjectId, config.Value.LexboxUrl);

    var miniLcmApi = await services.OpenCrdtProject(crdtProject);
    var crdtSyncService = services.GetRequiredService<CrdtSyncService>();
    await crdtSyncService.Sync();

    var result = await syncService.Sync(miniLcmApi, fwdataApi, dryRun);
    logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);

    await crdtSyncService.Sync();
    var srResult2 = await srService.SendReceive(fwDataProject, projectCode);
    logger.LogInformation("Send/Receive result after CRDT sync: {srResult2}", srResult2.Output);
    return TypedResults.Ok(result);
}

static async Task<Results<Ok<ProjectSyncStatus>, NotFound>> GetMergeStatus(
    ProjectLookupService projectLookupService,
    ProjectSyncStatusService syncStatusService,
    Guid projectId)
{
    var status = syncStatusService.SyncStatus(projectId);
    if (status is not null) return TypedResults.Ok(status.Value);
    // 404 only means "project doesn't exist"; if we don't know the status, then it hasn't synced before and is therefore ready to sync
    if (await projectLookupService.ProjectExists(projectId)) return TypedResults.Ok(ProjectSyncStatus.ReadyToSync);
    else return TypedResults.NotFound();
}

static async Task<FwDataMiniLcmApi> SetupFwData(FwDataProject fwDataProject,
    SendReceiveService srService,
    string projectCode,
    ILogger<Program> logger,
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

