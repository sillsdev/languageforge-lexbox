using System.Text.Json.Serialization;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;

namespace FwLiteShared.LocalSync;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OpenFromFolderOutcome
{
    /// <summary>Neither the folder, its subfolders nor its <c>FieldWorks Lite</c> folder hold a project.</summary>
    NoProjectFound,
    /// <summary>More than one project; <see cref="OpenFromFolderResult.ProjectNames"/> says which, so the user can pick one's folder.</summary>
    SeveralProjects,
    /// <summary>This device didn't have the project; it was created from the folder.</summary>
    Downloaded,
    /// <summary>This device had the project; it was synced with the folder.</summary>
    Synced,
    /// <summary>This device has the project, but the sync was refused (see <see cref="OpenFromFolderResult.Sync"/>).</summary>
    Refused,
    /// <summary>A different local project already uses the folder project's code.</summary>
    ProjectCodeInUse,
    /// <summary>A newer version of the app wrote this folder.</summary>
    NewerAppNeeded,
}

public record OpenFromFolderResult(OpenFromFolderOutcome Outcome, string? ProjectCode, FolderSyncResult? Sync = null, string[]? ProjectNames = null);

/// <summary>
/// "Open from folder" on the home screen: the user picks a drive, a <c>FieldWorks Lite</c> folder or a project folder
/// without having a project open.
/// </summary>
public class FolderProjectOpener(CrdtProjectsService projectsService, IServiceProvider services)
{
    public async Task<OpenFromFolderResult> OpenFromFolder(string path)
    {
        var found = SyncFolder.ProjectFoldersIn(path).ToArray();
        var projects = found
            .Select(f => (f.Path, Manifest: (f.State as SyncFolderState.HoldsProject)?.Manifest))
            .Where(f => f.Manifest is not null)
            .ToArray();
        if (projects.Length == 0)
        {
            return new OpenFromFolderResult(found.Length > 0 ? OpenFromFolderOutcome.NewerAppNeeded : OpenFromFolderOutcome.NoProjectFound, null);
        }
        if (projects.Length > 1)
        {
            return new OpenFromFolderResult(OpenFromFolderOutcome.SeveralProjects, null,
                ProjectNames: [.. projects.Select(p => p.Manifest!.ProjectName).Order()]);
        }

        var (folderPath, manifest) = (projects[0].Path, projects[0].Manifest!);
        await projectsService.EnsureProjectDataCacheIsLoaded();
        var existing = projectsService.GetProject(manifest.ProjectId);
        if (existing is not null)
        {
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(existing);
            var sync = await scope.ServiceProvider.GetRequiredService<FolderSyncService>().SyncWithFolder(folderPath);
            return new OpenFromFolderResult(sync.Synced ? OpenFromFolderOutcome.Synced : OpenFromFolderOutcome.Refused,
                existing.Name, sync);
        }

        if (projectsService.ProjectExists(manifest.ProjectCode))
            return new OpenFromFolderResult(OpenFromFolderOutcome.ProjectCodeInUse, manifest.ProjectCode);

        // like a Lexbox download: create the project from the manifest, then fill it as part of creation
        await projectsService.CreateProject(new(manifest.ProjectName,
            manifest.ProjectCode,
            manifest.ProjectId,
            manifest.OriginDomain is null ? null : new Uri(manifest.OriginDomain),
            AfterCreate: async (provider, _) => await provider.GetRequiredService<FolderSyncService>().DownloadFromFolder(folderPath)));
        return new OpenFromFolderResult(OpenFromFolderOutcome.Downloaded, manifest.ProjectCode);
    }
}
