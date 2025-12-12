using LexCore.Exceptions;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

public class ProjectDeletionService(
    IOptions<FwHeadlessConfig> config,
    ProjectLookupService projectLookupService,
    SyncHostedService syncHostedService,
    ILogger<ProjectDeletionService> logger)
{
    private readonly FwHeadlessConfig _config = config.Value;

    /// <summary>
    /// Deletes only the FwData repo folder (Mercurial repository).
    /// Preserves the CRDT database and project snapshot.
    /// </summary>
    public async Task<bool> DeleteRepo(Guid projectId)
    {
        AssertSyncNotInProgress(projectId);
        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null)
        {
            logger.LogInformation("DELETE repo request for non-existent project {ProjectId}", projectId);
            return false;
        }

        var fwDataFolder = _config.GetFwDataProject(projectCode, projectId).ProjectFolder;
        if (Directory.Exists(fwDataFolder))
        {
            logger.LogInformation("Deleting repo for project {ProjectCode} ({ProjectId})", projectCode, projectId);
            try
            {
                await Task.Run(() => Directory.Delete(fwDataFolder, true));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete repo for project {ProjectCode} ({ProjectId})", projectCode, projectId);
                throw;
            }
        }
        else
        {
            logger.LogInformation("Repo for project {ProjectCode} ({ProjectId}) does not exist", projectCode, projectId);
        }
        return true;
    }

    /// <summary>
    /// Deletes the entire project folder including FwData repo, CRDT database, and snapshots.
    /// </summary>
    public async Task<bool> DeleteProject(Guid projectId)
    {
        AssertSyncNotInProgress(projectId);
        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null)
        {
            logger.LogInformation("DELETE project request for non-existent project {ProjectId}", projectId);
            return false;
        }

        var projectFolder = _config.GetProjectFolder(projectCode, projectId);
        if (Directory.Exists(projectFolder))
        {
            logger.LogInformation("Deleting entire project folder for project {ProjectCode} ({ProjectId})", projectCode, projectId);
            try
            {
                await Task.Run(() => Directory.Delete(projectFolder, true));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete project for project {ProjectCode} ({ProjectId})", projectCode, projectId);
                throw;
            }
        }
        else
        {
            logger.LogInformation("Project folder for project {ProjectCode} ({ProjectId}) does not exist", projectCode, projectId);
        }
        return true;
    }

    private void AssertSyncNotInProgress(Guid projectId)
    {
        if (syncHostedService.IsJobQueuedOrRunning(projectId))
        {
            throw new ProjectSyncInProgressException(projectId);
        }
    }
}
