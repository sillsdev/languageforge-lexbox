using Microsoft.Extensions.Options;
using MiniLcm.Project;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Resource;
using LcmCrdt.RemoteSync;
using Microsoft.Extensions.Logging;
using MiniLcm.Media;

namespace LcmCrdt.MediaServer;

public class LcmMediaService(
    ResourceService resourceService,
    CurrentProjectService currentProjectService,
    IOptions<CrdtConfig> options,
    IRefitHttpServiceFactory refitFactory,
    IServerHttpClientProvider httpClientProvider,
    ILogger<LcmMediaService> logger
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
        var mediaClient = await MediaServerClient();
        var response = await mediaClient.DownloadFile(fileId);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to download file {fileId}: {response.StatusCode} {response.ReasonPhrase}");
        }
        return (await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentDisposition?.FileName?.Replace("\"", ""));
    }

    private async Task<IMediaServerClient> MediaServerClient()
    {
        var httpClient = await httpClientProvider.GetHttpClient();
        var mediaClient = refitFactory.Service<IMediaServerClient>(httpClient);
        return mediaClient;
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


    async Task<UploadResult> IRemoteResourceService.UploadResource(Guid resourceId, string localPath)
    {
        var mediaClient = await MediaServerClient();
        var fileName = Path.GetFileName(localPath);
        await mediaClient.UploadFile(
            new FileInfoPart(new FileInfo(localPath), fileName),
            projectId: currentProjectService.ProjectData.Id,
            fileId: resourceId.ToString("D"),
            filename: fileName);
        return new UploadResult(resourceId.ToString("N"));
    }

    public async Task<HarmonyResource> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        var projectResourceCachePath = ProjectResourceCachePath;
        Directory.CreateDirectory(projectResourceCachePath);
        var localPath = Path.Combine(projectResourceCachePath, Path.GetFileName(metadata.Filename));
        localPath = EnsureUnique(localPath);
        //must scope just to the copy, otherwise we can't upload the file to the server
        await using (var localFile = File.Create(localPath))
        {
            await stream.CopyToAsync(localFile);
        }

        try
        {
            IRemoteResourceService? remoteResourceService = null;
            if (await httpClientProvider.ConnectionStatus() == ConnectionStatus.Online) remoteResourceService = this;
            return await resourceService.AddLocalResource(
                localPath,
                currentProjectService.ProjectData.ClientId,
                resourceService: remoteResourceService
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to record file {Filename}", metadata.Filename);
            File.Delete(localPath);
            throw;
        }
    }

    private string EnsureUnique(string filePath)
    {
        if (!File.Exists(filePath)) return filePath;
        var directory = Path.GetDirectoryName(filePath);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        var filename = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var counter = 1;
        while (File.Exists(filePath))
        {
            filePath = Path.Combine(directory, $"{filename}-{counter}{extension}");
            counter++;
        }
        return filePath;
    }

    public async Task<bool> UploadPendingResources()
    {
        if (await httpClientProvider.ConnectionStatus() != ConnectionStatus.Online) return false;
        await resourceService.UploadPendingResources(currentProjectService.ProjectData.ClientId, this);
        return true;
    }
}
