using System.Runtime.CompilerServices;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using SIL.LCModel;

namespace LcmDebugger;

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

    public static async Task SyncDownloadedProject(this IServiceProvider services, string relativePath, bool dryRun = true, string? downloadsRoot = null)
    {
        // Default to a path relative to the executing assembly, pointing to the deployment/_downloads folder
        var fwHeadlessRoot = downloadsRoot ?? GetDefaultDownloadsPath();
        var currProjRoot = Path.Combine(fwHeadlessRoot, relativePath);
        var fwDataProject = new FwDataProject("fw", currProjRoot);

        var fwDataMiniLcmApi = services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);
        Console.WriteLine($"Project ID: {fwDataMiniLcmApi.ProjectId}");

        var crdtDbPath = Path.Combine(currProjRoot, "crdt.sqlite");
        var crdtProject = new CrdtProject("unused-project-code", crdtDbPath);
        var crdtMiniLcmApi = (CrdtMiniLcmApi)await services.GetRequiredService<CrdtProjectsService>().OpenProject(crdtProject, services);
        Console.WriteLine($"Crdt Project: {crdtMiniLcmApi.ProjectData.Code}");

        var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();

        var result = await syncService.Sync(crdtMiniLcmApi, fwDataMiniLcmApi, dryRun);
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
