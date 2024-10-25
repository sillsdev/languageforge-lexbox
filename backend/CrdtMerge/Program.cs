using CrdtMerge;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcm;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCrdtMerge();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    //access at /scalar/v1
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/sync", ExecuteMergeRequest);

app.Run();

static async Task<CrdtFwdataProjectSyncService.SyncResult> ExecuteMergeRequest(
    ILogger<Program> logger,
    IServiceProvider services,
    SendReceiveService srService,
    IOptions<CrdtMergeConfig> config,
    FwDataFactory fwDataFactory,
    ProjectsService projectsService,
    CrdtFwdataProjectSyncService syncService,
    string projectCode,
    // string projectName, // TODO: Add this to the API eventually
    bool dryRun = false)
{
    logger.LogInformation("About to execute sync request for {projectCode}", projectCode);
    if (dryRun)
    {
        logger.LogInformation("Dry run, not actually syncing");
        return new(0, 0);
    }

    // TODO: Instead of projectCode here, we'll evetually look up project ID and use $"{projectName}-{projectId}" as the project folder
    var projectFolder = Path.Join(config.Value.ProjectStorageRoot, projectCode);
    if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);

    // TODO: add projectName parameter and use it instead of projectCode here
    var crdtFile = Path.Join(projectFolder, $"{projectCode}.sqlite");

    var fwDataProject = new FwDataProject(projectCode, projectFolder); // TODO: use projectName (once we have it) instead of projectCode here
    logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
    logger.LogDebug("fwDataFile: {fwDataFile}", fwDataProject.FilePath);

    if (File.Exists(fwDataProject.FilePath))
    {
        var srResult = srService.SendReceive(fwDataProject);
        logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
    }
    else
    {
        var srResult = srService.Clone(fwDataProject);
        logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
    }
    var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, true);
    // var crdtProject = projectsService.GetProject(crdtProjectName);
    var crdtProject = File.Exists(crdtFile) ?
        new CrdtProject(projectCode, crdtFile) : // TODO: use projectName (once we have it) instead of projectCode here
        await projectsService.CreateProject(new(projectCode, fwdataApi.ProjectId, SeedNewProjectData: false, Path: projectFolder));
    var miniLcmApi = await services.OpenCrdtProject(crdtProject);
    var result = await syncService.Sync(miniLcmApi, fwdataApi, dryRun);
    logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
    var srResult2 = srService.SendReceive(fwDataProject);
    logger.LogInformation("Send/Receive result after CRDT sync: {srResult2}", srResult2.Output);
    return result;
}
