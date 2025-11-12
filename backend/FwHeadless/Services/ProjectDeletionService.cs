using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

public class ProjectSyncInProgressException(Guid projectId)
    : InvalidOperationException($"Cannot delete project {projectId} while sync job is running")
{
}

/// <summary>
/// Service for deleting FwHeadless project data.
/// </summary>
public class ProjectDeletionService(
    IOptions<FwHeadlessConfig> config,
    ProjectLookupService projectLookupService,
    SyncJobStatusService syncJobStatusService,
    ILogger<ProjectDeletionService> logger)
{
    private readonly FwHeadlessConfig _config = config.Value;

    /// <summary>
    /// Deletes only the FwData repo folder (Mercurial repository).
    /// Preserves the CRDT database and project snapshot.
    /// </summary>
    public async Task<bool> DeleteRepo(Guid projectId)
    {
        var projectCode = await ValidateAndGetProjectCode(projectId, "repo");
        if (projectCode is null) return false;

        var fwDataFolder = _config.GetFwDataProject(projectCode, projectId).ProjectFolder;
        DeleteFolderIfExists(fwDataFolder, "repository", projectCode, projectId);
        return true;
    }

    /// <summary>
    /// Deletes the entire project folder including FwData repo, CRDT database, and snapshots.
    /// </summary>
    public async Task<bool> DeleteProject(Guid projectId)
    {
        var projectCode = await ValidateAndGetProjectCode(projectId, "project");
        if (projectCode is null) return false;

        var projectFolder = _config.GetProjectFolder(projectCode, projectId);
        DeleteFolderIfExists(projectFolder, "project", projectCode, projectId);
        return true;
    }

    private async Task<string?> ValidateAndGetProjectCode(Guid projectId, string resourceType)
    {
        if (syncJobStatusService.SyncStatus(projectId) is SyncJobStatus.Running)
        {
            throw new ProjectSyncInProgressException(projectId);
        }

        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null)
        {
            logger.LogInformation("DELETE {ResourceType} request for non-existent project {ProjectId}", resourceType, projectId);
        }
        return projectCode;
    }

    private void DeleteFolderIfExists(string folderPath, string resourceName, string projectCode, Guid projectId)
    {
        if (Directory.Exists(folderPath))
        {
            logger.LogInformation("Deleting {ResourceName} for project {ProjectCode} ({ProjectId})", resourceName, projectCode, projectId);
            Directory.Delete(folderPath, true);
        }
        else
        {
            logger.LogInformation("{ResourceName} for project {ProjectCode} ({ProjectId}) does not exist", resourceName, projectCode, projectId);
        }
    }
}
