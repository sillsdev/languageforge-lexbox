using System.Runtime.CompilerServices;
using System.Text.Json;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteProjectSync;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIL.Harmony;
using SIL.LCModel;

namespace LcmDebugger;

public record FwHeadlessProject(CrdtMiniLcmApi CrdtApi, FwDataMiniLcmApi FwApi, string Name) : IDisposable
{
    public void Dispose()
    {
        CrdtApi.Dispose();
        FwApi.Dispose();
    }
}

public static class Utils
{
    private static ILogger Logger(this IServiceProvider services) =>
        services.GetRequiredService<ILoggerFactory>().CreateLogger("LcmDebugger");

    public static LcmCache? LoadProject(this IServiceProvider services, FwDataProject project)
    {
        var projectLoader = services.GetRequiredService<IProjectLoader>();
        var projectService = projectLoader.LoadCache(project);
        return projectService;
    }

    public static ILexEntry GetLexEntry(this IServiceProvider services, FwDataProject project, Guid entryId)
    {
        var cache = LoadProject(services, project) ?? throw new InvalidOperationException("Project not found.");
        var entryRepo = cache.ServiceLocator.GetInstance<ILexEntryRepository>() ?? throw new InvalidOperationException("Entry repository not found.");
        return entryRepo.GetObject(entryId) ?? throw new InvalidOperationException(message: "Entry not found.");
    }

    public static async Task PrintAllEntries(this IServiceProvider services, string code)
    {
        var projectList = services.GetRequiredService<FieldWorksProjectList>();
        var fwDataProject = projectList.GetProject(code);
        if (fwDataProject is null) throw new InvalidOperationException($"project {code} not found");
        var api = projectList.OpenProject(fwDataProject);
        await foreach (var entry in api.GetEntries())
        {
            Console.WriteLine(entry.Headword());
        }
    }

    public static async Task<CrdtMiniLcmApi> NewProjectFromSyncable(this IServiceProvider services, ISyncable syncable, Guid? projectId = null)
    {
        var crdtProjectsService = services.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateProject(new CrdtProjectsService.CreateProjectRequest("test-project", $"test-{Guid.NewGuid().ToString().Split('-')[0]}", projectId));
        var crdtMiniLcmApi = (CrdtMiniLcmApi)await crdtProjectsService.OpenProject(crdtProject, services);
        var syncResult = await services.GetRequiredService<DataModel>().SyncWith(syncable);
        if (!syncResult.IsSynced)
            throw new InvalidOperationException("New project sync failed.");
        return crdtMiniLcmApi;
    }

    public static async Task<FwHeadlessProject> OpenDownloadedProject(this IServiceProvider services, string relativePath, bool openCopy = false, string? downloadsRoot = null)
    {
        // Default to a path relative to the executing assembly, pointing to the deployment/_downloads folder
        var fwHeadlessRoot = downloadsRoot ?? GetDefaultDownloadsPath();
        var currProjRoot = Path.Combine(fwHeadlessRoot, relativePath);

        if (openCopy)
        {
            // Make a copy of the project to avoid modifying the original download
            var tempDir = Path.Combine(Path.GetTempPath(), $"{relativePath}_{Guid.NewGuid().ToString().Split('-')[0]}");
            Directory.CreateDirectory(tempDir);
            services.Logger().LogInformation("Copying project to temporary directory: {TempDir}", tempDir);
            LexCore.Utils.FileUtils.CopyFilesRecursively(new DirectoryInfo(currProjRoot), new DirectoryInfo(tempDir));
            currProjRoot = tempDir;
        }

        var fwDataProject = new FwDataProject("fw", currProjRoot);
        var fwDataMiniLcmApi = services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);
        services.Logger().LogInformation("Project ID: {ProjectId}", fwDataMiniLcmApi.ProjectId);

        var crdtDbPath = Path.Combine(currProjRoot, "crdt.sqlite");
        var crdtProject = new CrdtProject("unused-project-code", crdtDbPath);
        var crdtMiniLcmApi = (CrdtMiniLcmApi)await services.GetRequiredService<CrdtProjectsService>().OpenProject(crdtProject, services);
        services.Logger().LogInformation("Crdt Project: {Code}", crdtMiniLcmApi.ProjectData.Code);

        return new FwHeadlessProject(crdtMiniLcmApi, fwDataMiniLcmApi, relativePath);
    }

    public static async Task SyncFwHeadlessProject(this IServiceProvider services, FwHeadlessProject project, bool dryRun = true)
    {
        var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
        var snapshotService = services.GetRequiredService<ProjectSnapshotService>();
        var crdtMiniLcmApi = project.CrdtApi;
        var fwDataMiniLcmApi = project.FwApi;
        var projectSnapshot = await snapshotService.GetProjectSnapshot(fwDataMiniLcmApi.Project);
        SyncResult result;
        try
        {
            result = projectSnapshot is null
                        ? await syncService.Import(crdtMiniLcmApi, fwDataMiniLcmApi, dryRun)
                        : await syncService.Sync(crdtMiniLcmApi, fwDataMiniLcmApi, projectSnapshot, dryRun);
            if (!dryRun)
            {
                await snapshotService.RegenerateProjectSnapshot(crdtMiniLcmApi, fwDataMiniLcmApi.Project, keepBackup: false);
            }
        }
        catch (Exception e)
        {
            // the runtime prints an unhandled exception to stderr, which the file logger never sees
            services.Logger().LogError(e, "Sync failed");
            throw;
        }
        services.Logger().LogInformation("Sync completed successfully. Crdt changes: {CrdtChanges}, Fwdata changes: {FwdataChanges}.",
            result.CrdtChanges, result.FwdataChanges);

        // A dry run's whole output is its records; too much to read in a log, so they get their own file.
        if (result is CrdtFwdataProjectSyncService.DryRunSyncResult dryRunResult)
        {
            await services.WriteDryRunRecords(project.Name, "crdt", dryRunResult.CrdtDryRunRecords);
            await services.WriteDryRunRecords(project.Name, "fwdata", dryRunResult.FwDataDryRunRecords);
        }
    }

    private static async Task WriteDryRunRecords(this IServiceProvider services, string projectName, string side, List<RecordingMiniLcmApi.RunRecord> records)
    {
        var path = RunOutput.FilePath($"{projectName}-dry-run-{side}-records.json");
        await using var file = File.Create(path);
        await JsonSerializer.SerializeAsync(file, records, new JsonSerializerOptions { WriteIndented = true });
        services.Logger().LogInformation("Wrote {RecordCount} {Side} dry run records to {Path}", records.Count, side, path);
    }

    private static string GetDefaultDownloadsPath([CallerFilePath] string callerFilePath = "")
    {
        var sourceDir = Path.GetDirectoryName(callerFilePath) ??
            throw new InvalidOperationException("Could not determine source file directory");

        // Navigate up to find the repo root
        var currentDir = new DirectoryInfo(sourceDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "LexBox.slnx")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
            throw new InvalidOperationException("Could not find LexBox solution root from source file location");

        return Path.Combine(currentDir.FullName, "deployment", "_downloads");
    }

}

