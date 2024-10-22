using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
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

async Task ExecuteMergeRequest(ILogger<Program> logger, SendReceiveService srService, string projectCode, bool dryRun = false)
{
    logger.LogInformation($"About to execute sync request for {projectCode}");
    // var result = srService.SendReceive(projectCode);
    var result = srService.Clone(projectCode); // For testing, just clone into empty dir
    logger.LogInformation(result.Output);
}

// Version from command-line tool, commented out so I can reference it while I rewrite it to use services properly

// async Task ExecuteMergeRequest(string crdtFile, string fwDataFile, bool createCrdtDir, ILogger<Program> logger, bool dryRun = false)
// {
//     Console.WriteLine($"crdtFile: {crdtFile}");
//     Console.WriteLine($"fwDataFile: {fwDataFile}");
//     var fwProjectName = Path.GetFileNameWithoutExtension(fwDataFile);
//     var crdtProjectName = Path.GetFileNameWithoutExtension(crdtFile);

//     // TODO: Move this service root into the builder up above so we're not creating the services every time
//     await using var serviceRoot = SyncServices(crdtFile, fwDataFile, createCrdtDir);
//     await using var scope = serviceRoot.CreateAsyncScope();
//     var services = scope.ServiceProvider;
//     // var logger = services.GetRequiredService<ILogger<Program>>();
//     var fwdataApi = services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwProjectName, true);
//     var projectsService = services.GetRequiredService<ProjectsService>();
//     var crdtProject = projectsService.GetProject(crdtProjectName);
//     if (crdtProject is null)
//     {
//         crdtProject = await projectsService.CreateProject(new(crdtProjectName, fwdataApi.ProjectId, SeedNewProjectData: false));
//     }
//     projectsService.SetProjectScope(crdtProject);
//     await services.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
//     var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();

//     var result = await syncService.Sync(services.GetRequiredService<IMiniLcmApi>(), fwdataApi, dryRun);
//     logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
// };

// TODO: move this to own file so it can be an extension method on builder.Services
static void SyncServices(IServiceCollection crdtServices)
{
    crdtServices
        .AddLcmCrdtClient()
        .AddFwDataBridge()
        .AddFwLiteProjectSync()
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
    crdtServices.AddOptions<FwDataBridgeConfig>().Configure((FwDataBridgeConfig c, SRConfig srConfig) => c.ProjectsFolder = srConfig.FwDataProjectsFolder);
    crdtServices.AddOptions<LcmCrdtConfig>().Configure((LcmCrdtConfig c, SRConfig srConfig) => c.ProjectPath = srConfig.CrdtFolder);
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
