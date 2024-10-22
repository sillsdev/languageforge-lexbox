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
    SendReceiveService srService,
    IOptions<SRConfig> srConfig,
    FwDataFactory fwDataFactory,
    ProjectsService projectsService,
    CurrentProjectService currentProjectService,
    CrdtFwdataProjectSyncService syncService,
    IMiniLcmApi miniLcmApi,
    string projectCode,
    bool dryRun = true)
{
    logger.LogInformation("About to execute sync request for {projectCode}", projectCode);
    var cloneResult = srService.Clone(projectCode); // For testing, just clone into empty dir
    logger.LogInformation(cloneResult.Output);

    if (dryRun) return;

    var crdtFile = Path.Join(srConfig.Value.CrdtFolder, $"{projectCode}.fwdata"); // TODO: Determine what the correct filename is here
    var fwDataFile = Path.Join(srConfig.Value.FwDataProjectsFolder, $"{projectCode}.fwdata");
    Console.WriteLine($"crdtFile: {crdtFile}");
    Console.WriteLine($"fwDataFile: {fwDataFile}");
    var fwProjectName = projectCode;
    var crdtProjectName = projectCode;

    var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwProjectName, true);
    var crdtProject = projectsService.GetProject(crdtProjectName);
    if (crdtProject is null)
    {
        crdtProject = await projectsService.CreateProject(new(crdtProjectName, fwdataApi.ProjectId, SeedNewProjectData: false));
    }
    projectsService.SetProjectScope(crdtProject);
    await currentProjectService.PopulateProjectDataCache();

    var result = await syncService.Sync(miniLcmApi, fwdataApi, dryRun);
    // var srResult = srService.SendReceive(projectCode);
    logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);

}

// TODO: move this to own file so it can be an extension method on builder.Services
static void SyncServices(IServiceCollection crdtServices)
{
    crdtServices
        .AddLogging(builder => builder.AddConsole().AddDebug().AddConfiguration(new ConfigurationManager().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
        }).Build()));
    crdtServices.AddOptions<HgConfig>()
        .BindConfiguration("HgConfig")
        .ValidateDataAnnotations()
        .ValidateOnStart();
    crdtServices.AddOptions<SRConfig>()
        .BindConfiguration("SRConfig")
        .ValidateDataAnnotations()
        .ValidateOnStart();
    crdtServices.AddScoped<SendReceiveService>();
    crdtServices.AddOptions<FwDataBridgeConfig>().Configure((FwDataBridgeConfig c, IOptions<SRConfig> srConfig) => c.ProjectsFolder = srConfig.Value.FwDataProjectsFolder);
    crdtServices.AddOptions<LcmCrdtConfig>().Configure((LcmCrdtConfig c, IOptions<SRConfig> srConfig) => c.ProjectPath = srConfig.Value.CrdtFolder);
    crdtServices
        .AddLcmCrdtClient()
        .AddFwDataBridge()
        .AddFwLiteProjectSync();
}

public class HgConfig
{
    [Required]
    public required string RepoPath { get; init; }
    [Required]
    public required string SendReceiveDomain { get; init; }
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string HgWebUrl { get; init; }

    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string HgCommandServer { get; init; }
    [Required, Url]
    public required string HgResumableUrl { get; init; }
    public bool AutoUpdateLexEntryCountOnSendReceive { get; init; } = false;
    public bool RequireContainerVersionMatch { get; init; } = true;
    public int ResetCleanupAgeDays { get; init; } = 31;
}

public class SRConfig
{
    [Required]
    public required string LexboxUsername { get; init; }
    [Required]
    public required string LexboxPassword { get; init; }
    [Required]
    public required string CrdtFolder { get; init; }
    [Required]
    public required string FwDataProjectsFolder { get; init; }
    public string FdoDataModelVersion { get; init; } = "7000072";
}
