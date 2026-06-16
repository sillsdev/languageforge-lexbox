using Microsoft.JSInterop;
using LcmCrdt.MediaServer;
using SIL.Harmony.Resource;
using MiniLcm.Media;

namespace FwLiteShared.Services;

public class MediaFilesServiceJsInvokable(LcmMediaService mediaService)
{
    [JSInvokable]
    public async Task<HarmonyResource[]> AllResources()
    {
        return await mediaService.AllResources();
    }

    [JSInvokable]
    public async Task DownloadResources(IEnumerable<Guid> resourceIds)
    {
        await mediaService.DownloadResources(resourceIds);
    }

    [JSInvokable]
    public async Task UploadPendingResources()
    {
        await mediaService.UploadPendingResources();
    }

    [JSInvokable]
    public async Task<LcmFileMetadata> GetFileMetadata(Guid fileId)
    {
        return await mediaService.GetFileMetadata(fileId);
    }
}