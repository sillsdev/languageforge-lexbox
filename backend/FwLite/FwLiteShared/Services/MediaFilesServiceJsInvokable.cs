using Microsoft.JSInterop;
using LcmCrdt.MediaServer;
using SIL.Harmony.Resource;
using MiniLcm.Media;

namespace FwLiteShared.Services;

public class MediaFilesServiceJsInvokable(LcmMediaService mediaService)
{
    [JSInvokable]
    public async Task<RemoteResource[]> ResourcesPendingDownload()
    {
        return await mediaService.ResourcesPendingDownload();
    }

    [JSInvokable]
    public async Task<LocalResource[]> ResourcesPendingUpload()
    {
        return await mediaService.ResourcesPendingUpload();
    }

    [JSInvokable]
    public async Task DownloadAllResources()
    {
        await mediaService.DownloadAllResources();
    }

    [JSInvokable]
    public async Task UploadAllResources()
    {
        await mediaService.UploadAllResources();
    }

    [JSInvokable]
    public async Task DownloadResources(IEnumerable<RemoteResource> resources)
    {
        await mediaService.DownloadResources(resources);
    }

    [JSInvokable]
    public async Task UploadResources(IEnumerable<LocalResource> resources)
    {
        await mediaService.UploadResources(resources);
    }

    [JSInvokable]
    public async Task<LcmFileMetadata> GetFileMetadata(Guid fileId)
    {
        return await mediaService.GetFileMetadata(fileId);
    }
}
