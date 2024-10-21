using FwDataMiniLcmBridge;
using LcmCrdt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/sync", async (string crdtFile, string fwDataFile, bool createCrdtDir, bool dryRun = false) =>
{
    await ExecuteMergeRequest(crdtFile, fwDataFile, createCrdtDir, dryRun);
});

app.Run();

async Task ExecuteMergeRequest(string crdtFile, string fwDataFile, bool createCrdtDir, bool dryRun = false)
{
    Console.WriteLine($"crdtFile: {crdtFile}");
    Console.WriteLine($"fwDataFile: {fwDataFile}");
    var fwProjectName = Path.GetFileNameWithoutExtension(fwDataFile);
    var crdtProjectName = Path.GetFileNameWithoutExtension(crdtFile);

    // TODO: Move this service root into the builder up above so we're not creating the services every time
    await using var serviceRoot = SyncServices(crdtFile, fwDataFile, createCrdtDir);
    await using var scope = serviceRoot.CreateAsyncScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var fwdataApi = services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwProjectName, true);
    var projectsService = services.GetRequiredService<ProjectsService>();
    var crdtProject = projectsService.GetProject(crdtProjectName);
    if (crdtProject is null)
    {
        crdtProject = await projectsService.CreateProject(new(crdtProjectName, fwdataApi.ProjectId, SeedNewProjectData: false));
    }
    projectsService.SetProjectScope(crdtProject);
    await services.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
    var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();

    var result = await syncService.Sync(services.GetRequiredService<IMiniLcmApi>(), fwdataApi, dryRun);
    logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
};

static ServiceProvider SyncServices(string crdtFile, string fwDataFile, bool createCrdtDir)
{
    if (!File.Exists(fwDataFile)) throw new InvalidOperationException("Could not find fwdata file " + fwDataFile);

    var fwDataProjectsFolder = Directory.GetParent(fwDataFile)?.Parent;
    if (fwDataProjectsFolder is null) throw new InvalidOperationException("Could not find parent folder of fwdata dir " + fwDataFile);
    var crdtFolder = Directory.GetParent(crdtFile);
    if (crdtFolder is null) throw new InvalidOperationException("Could not find parent folder of crdt file " + crdtFile);
    if (!crdtFolder.Exists && createCrdtDir)
    {
        crdtFolder.Create();
    } else if (!crdtFolder.Exists)
    {
        throw new InvalidOperationException("Could not find crdt folder " + crdtFolder);
    }

    var crdtServices = new ServiceCollection()
        .AddLcmCrdtClient()
        .AddFwDataBridge()
        .AddSingleton<CrdtFwdataProjectSyncService>()
        .AddSingleton<MiniLcmImport>()
        .Configure<FwDataBridgeConfig>(c => c.ProjectsFolder = fwDataProjectsFolder.FullName)
        .Configure<LcmCrdtConfig>(c => c.ProjectPath = crdtFolder.FullName)
        .AddLogging(builder => builder.AddConsole().AddDebug().AddConfiguration(new ConfigurationManager().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
        }).Build()))
        .BuildServiceProvider(true);
    return crdtServices;
}

// TODO: Change the MapPost to take this type instead:
record CrdtMergeRequest(string crdtFile, string fwDataFile, bool createCrdtDir, bool dryRun = false);
