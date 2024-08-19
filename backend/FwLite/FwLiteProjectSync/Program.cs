using System.CommandLine;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm;


var rootCommand = new RootCommand("CRDT to FwData sync tool");
var crdtOption = new Option<string>("--crdt", "CRDT sqlite file") { IsRequired = true };
rootCommand.AddGlobalOption(crdtOption);
var fwDataOption = new Option<string>("--fwdata", "FwData file") { IsRequired = true };
rootCommand.AddGlobalOption(fwDataOption);
var beforeSyncCommand = new Command("before-sync", "Run before sync");
beforeSyncCommand.SetHandler((crdtFile, fwDataFile) =>
    {
        Console.WriteLine($"crdtFile: {crdtFile}");
        Console.WriteLine($"fwDataFile: {fwDataFile}");
    },
    crdtOption,
    fwDataOption);
rootCommand.Add(beforeSyncCommand);
var afterSyncCommand = new Command("after-sync", "Run after sync");
afterSyncCommand.SetHandler((crdtFile, fwDataFile) =>
    {
        Console.WriteLine($"crdtFile: {crdtFile}");
        Console.WriteLine($"fwDataFile: {fwDataFile}");
    },
    crdtOption,
    fwDataOption);
rootCommand.Add(afterSyncCommand);
var syncCommand = new Command("sync", "Synchronize CRDT with FwData");
var createCrdtDirOption = new Option<bool>("--create-crdt-dir", "Create CRDT directory");
syncCommand.Add(createCrdtDirOption);
syncCommand.SetHandler(async (crdtFile, fwDataFile, createCrdtDir) =>
    {
        Console.WriteLine($"crdtFile: {crdtFile}");
        Console.WriteLine($"fwDataFile: {fwDataFile}");
        var fwProjectName = Path.GetFileNameWithoutExtension(fwDataFile);
        var crdtProjectName = Path.GetFileNameWithoutExtension(crdtFile);

        await using var serviceRoot = SyncServices(crdtFile, fwDataFile, createCrdtDir);
        await using var scope = serviceRoot.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var fwdataApi = services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwProjectName, true);
        var projectsService = services.GetRequiredService<ProjectsService>();
        var crdtProject = projectsService.GetProject(crdtProjectName);
        if (crdtProject is null)
        {
            crdtProject = await projectsService.CreateProject(crdtProjectName, fwdataApi.ProjectId);
        }
        projectsService.SetProjectScope(crdtProject);
        await services.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
        var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();

        var result = await syncService.Sync(services.GetRequiredService<ILexboxApi>(), fwdataApi);
        logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
    },
    crdtOption,
    fwDataOption,
    createCrdtDirOption);
rootCommand.Add(syncCommand);
await rootCommand.InvokeAsync(args);

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
        .AddFwLiteProjectSync()
        .Configure<FwDataBridgeConfig>(c => c.ProjectsFolder = fwDataProjectsFolder.FullName)
        .Configure<LcmCrdtConfig>(c => c.ProjectPath = crdtFolder.FullName)
        .AddLogging(builder => builder.AddConsole().AddDebug())
        .BuildServiceProvider(true);
    return crdtServices;
}
