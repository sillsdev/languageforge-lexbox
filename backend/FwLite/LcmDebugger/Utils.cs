using System.Runtime.CompilerServices;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using SIL.LCModel;

namespace LcmDebugger;

public record FwHeadlessProject(CrdtMiniLcmApi CrdtApi, FwDataMiniLcmApi FwApi) : IDisposable
{
    public void Dispose()
    {
        CrdtApi.Dispose();
        FwApi.Dispose();
    }
}

public static class Utils
{
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
            Console.WriteLine($"Copying project to temporary directory: {tempDir}");
            LexCore.Utils.FileUtils.CopyFilesRecursively(new DirectoryInfo(currProjRoot), new DirectoryInfo(tempDir));
            currProjRoot = tempDir;
        }

        var fwDataProject = new FwDataProject("fw", currProjRoot);
        var fwDataMiniLcmApi = services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);
        Console.WriteLine($"Project ID: {fwDataMiniLcmApi.ProjectId}");

        var crdtDbPath = Path.Combine(currProjRoot, "crdt.sqlite");
        var crdtProject = new CrdtProject("unused-project-code", crdtDbPath);
        var crdtMiniLcmApi = (CrdtMiniLcmApi)await services.GetRequiredService<CrdtProjectsService>().OpenProject(crdtProject, services);
        Console.WriteLine($"Crdt Project: {crdtMiniLcmApi.ProjectData.Code}");

        return new FwHeadlessProject(crdtMiniLcmApi, fwDataMiniLcmApi);
    }

    public static async Task SyncFwHeadlessProject(this IServiceProvider services, FwHeadlessProject project, bool dryRun = true)
    {
        var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
        var snapshotService = services.GetRequiredService<ProjectSnapshotService>();
        var crdtMiniLcmApi = project.CrdtApi;
        var fwDataMiniLcmApi = project.FwApi;
        var projectSnapshot = await snapshotService.GetProjectSnapshot(fwDataMiniLcmApi.Project);
        var result = projectSnapshot is null
                    ? await syncService.Import(crdtMiniLcmApi, fwDataMiniLcmApi, dryRun)
                    : await syncService.Sync(crdtMiniLcmApi, fwDataMiniLcmApi, projectSnapshot, dryRun);
        if (!dryRun)
        {
            await snapshotService.RegenerateProjectSnapshot(crdtMiniLcmApi, fwDataMiniLcmApi.Project, keepBackup: false);
        }
        Console.WriteLine($"Sync completed successfully. Crdt changes: {result.CrdtChanges}, Fwdata changes: {result.FwdataChanges}.");
    }

    private static string GetDefaultDownloadsPath([CallerFilePath] string callerFilePath = "")
    {
        var sourceDir = Path.GetDirectoryName(callerFilePath) ??
            throw new InvalidOperationException("Could not determine source file directory");

        // Navigate up to find the repo root
        var currentDir = new DirectoryInfo(sourceDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "LexBox.sln")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
            throw new InvalidOperationException("Could not find LexBox solution root from source file location");

        return Path.Combine(currentDir.FullName, "deployment", "_downloads");
    }

}

