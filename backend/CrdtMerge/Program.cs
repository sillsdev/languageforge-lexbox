using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcm;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

SyncServices(builder.Services); // TODO: extension method

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/sync", ExecuteMergeRequest);

app.Run();

async Task ExecuteMergeRequest(
    ILogger<Program> logger,
    IServiceProvider services,
    SendReceiveService srService,
    IOptions<SRConfig> srConfig,
    FwDataFactory fwDataFactory,
    ProjectsService projectsService,
    CrdtFwdataProjectSyncService syncService,
    string projectCode,
    bool dryRun = false)
{
    logger.LogInformation("About to execute sync request for {projectCode}", projectCode);
    if (dryRun) {
        logger.LogInformation("Dry run, not actually cloning");
        return;
    }

    // TODO: Instead of projectCode here, we'll evetually look up project ID and use $"{projectName}-{projectId}" as the project folder
    var projectFolder = Path.Join(srConfig.Value.ProjectStorageRoot, projectCode);
    if (!Directory.Exists(projectFolder)) Directory.CreateDirectory(projectFolder);

    // TODO: add projectName parameter and use it instead of projectCode here
    var crdtFile = Path.Join(projectFolder, $"{projectCode}.sqlite");
    var fwDataFile = Path.Join(projectFolder, projectCode, $"{projectCode}.fwdata");
    logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
    logger.LogDebug("fwDataFile: {fwDataFile}", fwDataFile);
    var fwProjectName = projectCode;
    var crdtProjectName = projectCode;

    if (File.Exists(fwDataFile))
    {
        var srResult = srService.SendReceive(projectFolder, projectCode);
        logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
    }
    else
    {
        var srResult = srService.Clone(projectFolder, projectCode);
        logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
    }

    var fwDataProject = new FwDataProject(projectCode, fwDataFile, projectFolder); // TODO: use projectName (once we have it) instead of projectCode here
    var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, true);
    // var crdtProject = projectsService.GetProject(crdtProjectName);
    var crdtProject = File.Exists(crdtFile) ?
        new CrdtProject(projectCode, crdtFile) : // TODO: use projectName (once we have it) instead of projectCode here
        await projectsService.CreateProject(new(crdtProjectName, fwdataApi.ProjectId, SeedNewProjectData: false, Path: projectFolder));
    projectsService.SetProjectScope(crdtProject);
    var currentProjectService = services.GetRequiredService<CurrentProjectService>();
    await currentProjectService.PopulateProjectDataCache();
    var miniLcmApi = services.GetRequiredService<IMiniLcmApi>();
    var result = await syncService.Sync(miniLcmApi, fwdataApi, dryRun);
    logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
    var srResult2 = srService.SendReceive(projectFolder, projectCode);
    logger.LogInformation("Send/Receive result after CRDT sync: {srResult2}", srResult2.Output);
}

// TODO: move this to own file so it can be an extension method on builder.Services
static void SyncServices(IServiceCollection crdtServices)
{
    crdtServices
        .AddLogging(builder => builder.AddConsole().AddDebug().AddConfiguration(new ConfigurationManager().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
        }).Build()));
    crdtServices.AddOptions<SRConfig>()
        .BindConfiguration("SRConfig")
        .ValidateDataAnnotations()
        .ValidateOnStart();
    crdtServices.AddScoped<SendReceiveService>();
    crdtServices
        .AddLcmCrdtClient()
        .AddFwDataBridge()
        .AddFwLiteProjectSync();
}

public class SRConfig
{
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string LexboxUrl { get; init; }
    public string HgWebUrl => $"{LexboxUrl}hg/";
    [Required]
    public required string LexboxUsername { get; init; }
    [Required]
    public required string LexboxPassword { get; init; }
    [Required]
    public required string ProjectStorageRoot { get; init; }
    public string FdoDataModelVersion { get; init; } = "7000072";
}
