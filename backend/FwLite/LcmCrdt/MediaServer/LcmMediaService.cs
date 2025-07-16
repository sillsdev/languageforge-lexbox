using Microsoft.Extensions.Options;
using MiniLcm.Project;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Resource;
using LcmCrdt.RemoteSync;

namespace LcmCrdt.MediaServer;

public class LcmMediaService(
    ResourceService resourceService,
    CurrentProjectService currentProjectService,
    IOptions<CrdtConfig> options,
    IRefitHttpServiceFactory refitFactory,
    IServerHttpClientProvider httpClientProvider
) : IRemoteResourceService
{
    public async Task<HarmonyResource[]> AllResources()
    {
        return await resourceService.AllResources();
    }

    /// <summary>
    /// should only be used in fw-headless for files which already exist in the lexbox db
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="localPath"></param>
    public async Task AddExistingRemoteResource(Guid fileId, string localPath)
    {
        await resourceService.AddExistingRemoteResource(localPath,
            currentProjectService.ProjectData.ClientId,
            fileId,
            fileId.ToString("N"));
    }

    public async Task DeleteResource(Guid fileId)
    {
        await resourceService.DeleteResource(currentProjectService.ProjectData.ClientId, fileId);
    }

    /// <summary>
    /// return a stream for the file, if it's not cached locally, it will be downloaded
    /// </summary>
    /// <param name="fileId">media file Id</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<ReadFileResponse> GetFileStream(Guid fileId)
    {
        var localResource = await resourceService.GetLocalResource(fileId);
        if (localResource is null)
        {
            var connectionStatus = await httpClientProvider.ConnectionStatus();
            if (connectionStatus == ConnectionStatus.Online)
            {
                localResource = await resourceService.DownloadResource(fileId, this);
            }
            else
            {
                return new ReadFileResponse(ReadFileResult.Offline);
            }
        }
        //todo, consider trying to download the file again, maybe the cache was cleared
        if (!File.Exists(localResource.LocalPath))
            throw new FileNotFoundException("Unable to find the file with Id" + fileId, localResource.LocalPath);
        return new(File.OpenRead(localResource.LocalPath), Path.GetFileName(localResource.LocalPath));
    }

    private async Task<(Stream? stream, string? filename)> RequestMediaFile(Guid fileId)
    {
        var httpClient = await httpClientProvider.GetHttpClient();
        var mediaClient = refitFactory.Service<IMediaServerClient>(httpClient);
        var response = await mediaClient.DownloadFile(fileId);
        return (await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentDisposition?.FileName?.Replace("\"", ""));
    }

    async Task<DownloadResult> IRemoteResourceService.DownloadResource(string remoteId, string localResourceCachePath)
    {
        var projectResourceCachePath = ProjectResourceCachePath;
        Directory.CreateDirectory(projectResourceCachePath);
        var (stream, filename) = await RequestMediaFile(new Guid(remoteId));
        if (stream is null) throw new FileNotFoundException(remoteId);
        await using (stream)
        {
            filename = Path.GetFileName(filename);
            var localPath = Path.Combine(projectResourceCachePath, filename ?? remoteId);
            await using var localFile = File.OpenWrite(localPath);
            await stream.CopyToAsync(localFile);
            return new DownloadResult(localPath);
        }
    }

    public string ProjectResourceCachePath =>
        Path.Combine(options.Value.LocalResourceCachePath, currentProjectService.Project.Name);

    public Task<UploadResult> UploadResource(Guid resourceId, string localPath)
    {
        throw new NotImplementedException();
    }
}
