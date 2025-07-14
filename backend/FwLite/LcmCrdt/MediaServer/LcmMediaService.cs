using SIL.Harmony;
using SIL.Harmony.Resource;

namespace LcmCrdt.MediaServer;

public class LcmMediaService(ResourceService resourceService, CurrentProjectService projectService)
{
    public async Task<HarmonyResource[]> AllResources()
    {
        return await resourceService.AllResources();
    }

    public async Task AddExistingRemoteResource(Guid fileId, string localPath)
    {
        await resourceService.AddExistingRemoteResource(localPath, projectService.ProjectData.ClientId, fileId, fileId.ToString("N"));
    }

    public async Task DeleteResource(Guid fileId)
    {
        await resourceService.DeleteResource(projectService.ProjectData.ClientId, fileId);
    }
}
