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

    var crdtFile = Path.Join(srConfig.Value.CrdtFolder, projectCode, $"{projectCode}.sqlite");
    var fwDataFile = Path.Join(srConfig.Value.FwDataProjectsFolder, projectCode, $"{projectCode}.fwdata");
    logger.LogDebug("crdtFile: {crdtFile}", crdtFile);
    logger.LogDebug("fwDataFile: {fwDataFile}", fwDataFile);
    var fwProjectName = projectCode;
    var crdtProjectName = projectCode;

    if (!File.Exists(fwDataFile)) // Should only happen during local dev testing
    {
        var cloneResult = srService.Clone(projectCode);
        logger.LogInformation(cloneResult.Output);
    }

    var fwdataApi = fwDataFactory.GetFwDataMiniLcmApi(fwProjectName, true);
    var crdtProject = projectsService.GetProject(crdtProjectName);
    crdtProject ??= await projectsService.CreateProject(new(crdtProjectName, fwdataApi.ProjectId, SeedNewProjectData: false));
    projectsService.SetProjectScope(crdtProject);
    var currentProjectService = services.GetRequiredService<CurrentProjectService>();
    await currentProjectService.PopulateProjectDataCache();
    var miniLcmApi = services.GetRequiredService<IMiniLcmApi>();
    var result = await syncService.Sync(miniLcmApi, fwdataApi, dryRun);
    logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
    var srResult = srService.SendReceive(projectCode);
    logger.LogInformation("Send/Receive result: {srResult}", srResult.Output);
    if (srResult.Output.Contains("No changes from others"))
    {
        // No need for second sync if others didn't push any changes
        return;
    }
    var result2 = await syncService.Sync(miniLcmApi, fwdataApi, dryRun);
    logger.LogInformation("Second sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result2.CrdtChanges, result2.FwdataChanges);
    // TODO: Determine whether and how to combine those two results into one report, or report them separately
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
    crdtServices.AddOptions<FwDataBridgeConfig>().Configure((FwDataBridgeConfig c, IOptions<SRConfig> srConfig) => c.ProjectsFolder = srConfig.Value.FwDataProjectsFolder);
    crdtServices.AddOptions<LcmCrdtConfig>().Configure((LcmCrdtConfig c, IOptions<SRConfig> srConfig) => c.ProjectPath = srConfig.Value.CrdtFolder);
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
    [Required]
    public required string CrdtFolder { get; init; }
    [Required]
    public required string FwDataProjectsFolder { get; init; }
    public string FdoDataModelVersion { get; init; } = "7000072";
}
