using FwHeadless;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LexData;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using MiniLcm;
using Scalar.AspNetCore;

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    //access at /scalar/v1
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/api/healthz");

app.MapPost("/sync", ExecuteMergeRequest);

app.Run();

static async Task<Results<Ok<CrdtFwdataProjectSyncService.SyncResult>, NotFound>> ExecuteMergeRequest(
    ILogger<Program> logger,
    IServiceProvider services,
    SendReceiveService srService,
    IOptions<FwHeadlessConfig> config,
    FwDataFactory fwDataFactory,
    ProjectsService projectsService,
    ProjectLookupService projectLookupService,
    CrdtFwdataProjectSyncService syncService,
    Guid projectId,
    bool dryRun = false)
{
    logger.LogInformation("About to execute sync request for {projectId}", projectId);
    if (dryRun)
    {
        logger.LogInformation("Dry run, not actually syncing");
        return TypedResults.Ok(new CrdtFwdataProjectSyncService.SyncResult(0, 0));
    }

    var projectCode = await projectLookupService.GetProjectCode(projectId);
    if (projectCode is null)
    {
        logger.LogError("Project ID {projectId} not found", projectId);
        return TypedResults.NotFound();
    }
    logger.LogInformation("Project code is {projectCode}", projectCode);

    var projectFolder = Path.Join(config.Value.ProjectStorageRoot, $"{projectCode}-{projectId}");
    if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);

    var crdtFile = Path.Join(projectFolder, "crdt.sqlite");

    var fwDataProject = new FwDataProject("fw", projectFolder);
    logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
    logger.LogDebug("fwDataFile: {fwDataFile}", fwDataProject.FilePath);

    if (File.Exists(fwDataProject.FilePath))
    {
        var srResult = srService.SendReceive(fwDataProject, projectCode);
        logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
    }
    else
    {
        var srResult = srService.Clone(fwDataProject, projectCode);
        logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
    }
    var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, true);
    var crdtProject = File.Exists(crdtFile) ?
        new CrdtProject("crdt", crdtFile) :
        await projectsService.CreateProject(new("crdt", SeedNewProjectData: false, Path: projectFolder, FwProjectId: fwdataApi.ProjectId));
    var miniLcmApi = await services.OpenCrdtProject(crdtProject);
    var result = await syncService.Sync(miniLcmApi, fwdataApi, dryRun);
    logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
    var srResult2 = srService.SendReceive(fwDataProject, projectCode);
    logger.LogInformation("Send/Receive result after CRDT sync: {srResult2}", srResult2.Output);
    return TypedResults.Ok(result);
}

