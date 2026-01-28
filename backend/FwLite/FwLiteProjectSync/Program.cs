using System.CommandLine;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm;

namespace FwLiteProjectSync;

public class Program
{
    public static Task<int> Main(string[] args)
    {
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
        var dryRunOption = new Option<bool>("--dry-run", () => false, "Dry run");
        syncCommand.Add(createCrdtDirOption);
        syncCommand.Add(dryRunOption);
        syncCommand.SetHandler(async (crdtFile, fwDataFile, createCrdtDir, dryRun) =>
            {
                Console.WriteLine($"crdtFile: {crdtFile}");
                Console.WriteLine($"fwDataFile: {fwDataFile}");
                var fwProjectName = Path.GetFileNameWithoutExtension(fwDataFile);
                var crdtProjectCode = Path.GetFileNameWithoutExtension(crdtFile);

                await using var serviceRoot = SyncServices(crdtFile, fwDataFile, createCrdtDir);
                await using var scope = serviceRoot.CreateAsyncScope();
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                var fieldWorksProjectList = services.GetRequiredService<FieldWorksProjectList>();
                var fwProject = fieldWorksProjectList.GetProject(fwProjectName);
                if (fwProject is null)
                {
                    throw new InvalidOperationException("Could not find fwdata project " + fwProjectName);
                }
                var fwdataApi = (FwDataMiniLcmApi) fieldWorksProjectList.OpenProject(fwProject);
                var projectsService = services.GetRequiredService<CrdtProjectsService>();
                var crdtProject = projectsService.GetProject(crdtProjectCode);
                if (crdtProject is null)
                {
                    crdtProject = await projectsService.CreateProject(new(crdtProjectCode, crdtProjectCode, FwProjectId:fwdataApi.ProjectId, SeedNewProjectData: false));
                }
                var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
                var snapshotService = services.GetRequiredService<ProjectSnapshotService>();
                var crdtApi = await services.OpenCrdtProject(crdtProject);

                var projectSnapshot = await snapshotService.GetProjectSnapshot(fwdataApi.Project);
                SyncResult result;
                if (projectSnapshot is null)
                {
                    if (!File.Exists(crdtProject.DbPath))
                    {
                        result = await syncService.Import(crdtApi, fwdataApi, dryRun);
                    }
                    else
                    {
                        throw new InvalidOperationException("Project snapshot missing for existing CRDT project");
                    }
                }
                else
                {
                    result = await syncService.Sync(crdtApi, fwdataApi, projectSnapshot, dryRun);
                }
                logger.LogInformation("Sync result, CrdtChanges: {CrdtChanges}, FwdataChanges: {FwdataChanges}", result.CrdtChanges, result.FwdataChanges);
            },
            crdtOption,
            fwDataOption,
            createCrdtDirOption,
            dryRunOption);
        rootCommand.Add(syncCommand);
        return rootCommand.InvokeAsync(args);

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
                .AddLogging(builder => builder.AddConsole().AddDebug().AddConfiguration(new ConfigurationManager().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
                }).Build()))
                .BuildServiceProvider(true);
            return crdtServices;
        }
    }
}
