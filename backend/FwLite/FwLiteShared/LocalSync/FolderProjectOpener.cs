using System.Text.Json.Serialization;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;

namespace FwLiteShared.LocalSync;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OpenFromFolderOutcome
{
    EmptyFolder,
    NotASyncFolder,
    /// <summary>This device didn't have the project; it was created from the folder.</summary>
    Downloaded,
    /// <summary>This device already had the project. It's opened without syncing: syncing also writes to the folder.</summary>
    AlreadyOnDevice,
    /// <summary>A different local project already uses the folder project's code.</summary>
    ProjectCodeInUse,
    /// <summary>A newer version of the app wrote this folder.</summary>
    NewerAppNeeded,
}

public record OpenFromFolderResult(OpenFromFolderOutcome Outcome, string? ProjectCode, string? ProjectName = null);

/// <summary>
/// "Get project from folder" on the home screen: the user picks a sync folder without having a project open.
/// </summary>
public class FolderProjectOpener(CrdtProjectsService projectsService, IServiceProvider services)
{
    public async Task<OpenFromFolderResult> OpenFromFolder(string path)
    {
        path = Path.GetFullPath(path);
        var state = SyncFolder.Inspect(path);
        if (state is not SyncFolderState.HoldsProject { Manifest: var manifest })
        {
            var outcome = state switch
            {
                SyncFolderState.Empty => OpenFromFolderOutcome.EmptyFolder,
                SyncFolderState.NewerFormat => OpenFromFolderOutcome.NewerAppNeeded,
                _ => OpenFromFolderOutcome.NotASyncFolder,
            };
            return new OpenFromFolderResult(outcome, null);
        }

        await projectsService.EnsureProjectDataCacheIsLoaded();
        var existing = projectsService.GetProject(manifest.ProjectId);
        if (existing is not null)
            return new OpenFromFolderResult(OpenFromFolderOutcome.AlreadyOnDevice, existing.Name, manifest.ProjectName);

        if (projectsService.ProjectExists(manifest.ProjectCode))
            return new OpenFromFolderResult(OpenFromFolderOutcome.ProjectCodeInUse, manifest.ProjectCode, manifest.ProjectName);

        // like a Lexbox download: create the project from the manifest, then fill it as part of creation
        await projectsService.CreateProject(new(manifest.ProjectName,
            manifest.ProjectCode,
            manifest.ProjectId,
            manifest.OriginDomain is null ? null : new Uri(manifest.OriginDomain),
            AfterCreate: async (provider, _) => await provider.GetRequiredService<FolderSyncService>().DownloadFromFolder(path)));
        return new OpenFromFolderResult(OpenFromFolderOutcome.Downloaded, manifest.ProjectCode, manifest.ProjectName);
    }

    /// <summary>The "Sync now" offered after <see cref="OpenFromFolderOutcome.AlreadyOnDevice"/>.</summary>
    public async Task<FolderSyncResult> SyncWithFolder(string projectCode, string path)
    {
        await projectsService.EnsureProjectDataCacheIsLoaded();
        var project = projectsService.GetProject(projectCode)
                      ?? throw new InvalidOperationException($"Project '{projectCode}' not found");
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        return await scope.ServiceProvider.GetRequiredService<FolderSyncService>().SyncWithFolder(path);
    }
}
