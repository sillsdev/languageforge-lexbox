using Microsoft.Extensions.Options;
using MiniLcm.Project;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Resource;
using LcmCrdt.RemoteSync;
using Microsoft.Extensions.Logging;
using MiniLcm.Media;
using System.Net.Http.Json;

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

    public async Task<RemoteResource[]> ResourcesPendingDownload()
    {
        return await resourceService.ListResourcesPendingDownload();
    }

    public async Task<LocalResource[]> ResourcesPendingUpload()
    {
        return await resourceService.ListResourcesPendingUpload();
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

    public async Task<LocalResource?> DownloadResourceIfNeeded(Guid fileId)
    {
        var localResource = await resourceService.GetLocalResource(fileId);
        if (localResource is null)
        {
            var connectionStatus = await httpClientProvider.ConnectionStatus();
            if (connectionStatus == ConnectionStatus.Online)
            {
                return await resourceService.DownloadResource(fileId, this);
            }
        }
        return localResource;
    }

    public async Task DownloadAllResources()
    {
        var resources = await ResourcesPendingDownload();
        var localResourceCachePath = options.Value.LocalResourceCachePath;
        foreach (var resource in resources)
        {
            if (resource.RemoteId is null) continue;
            await ((IRemoteResourceService)this).DownloadResource(resource.RemoteId, localResourceCachePath);
            // NOTE: DownloadResource never uses the localResourceCachePath parameter; bug? Or just a quirk of how the API works?
        }
    }

    public async Task UploadAllResources()
    {
        await UploadResources(await ResourcesPendingUpload());
    }

    public async Task DownloadResources(IEnumerable<RemoteResource> resources)
    {
        foreach (var resource in resources)
        {
            await DownloadResourceIfNeeded(resource.Id);
        }
    }

    public async Task UploadResources(IEnumerable<LocalResource> resources)
    {
        foreach (var resource in resources)
        {
            await ((IRemoteResourceService)this).UploadResource(resource.Id, resource.LocalPath);
        }
    }

    /// <summary>
    /// return a stream for the file, if it's not cached locally, it will be downloaded
    /// </summary>
    /// <param name="fileId">media file Id</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<ReadFileResponse> GetFileStream(Guid fileId)
    {
        var localResource = await DownloadResourceIfNeeded(fileId);
        if (localResource is null)
        {
            var connectionStatus = await httpClientProvider.ConnectionStatus();
            if (connectionStatus == ConnectionStatus.Online)
            {
                // Try again, maybe earlier failure was a blip
                localResource = await DownloadResourceIfNeeded(fileId);
            }
            else
            {
                return new ReadFileResponse(ReadFileResult.Offline);
            }
        }
        if (localResource is null || !File.Exists(localResource.LocalPath))
        {
            // One more attempt to download again, maybe the cache was cleared
            localResource = await DownloadResourceIfNeeded(fileId);
            // If still null then connection is offline or unreliable enough to consider as offline
            if (localResource is null) return new ReadFileResponse(ReadFileResult.Offline);
        }
        // If still can't find local path then this is where we give up
        if (!File.Exists(localResource.LocalPath))
            throw new FileNotFoundException("Unable to find the file with Id" + fileId, localResource.LocalPath);
        return new(File.OpenRead(localResource.LocalPath), Path.GetFileName(localResource.LocalPath));
    }

    public async Task<LcmFileMetadata> GetFileMetadata(Guid fileId)
    {
        var mediaClient = await MediaServerClient();
        var response = await mediaClient.GetFileMetadata(fileId);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to retrieve metadata for file {fileId}: {response.StatusCode} {response.ReasonPhrase}");
        }
        var metadata = await response.Content.ReadFromJsonAsync<LcmFileMetadata>();
        if (metadata is null)
        {
            // Try to get content into error message, but if buffering not enabled for this request, give up
            var content = "";
            try
            {
                content = await response.Content.ReadAsStringAsync();
            }
            catch { } // Oh well, we tried
            throw new Exception($"Failed to retrieve metadata for file {fileId}: response was in incorrect format. {content}");
        }
        return metadata;
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
            localPath = EnsureUnique(localPath);
            await using var localFile = File.Create(localPath);
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

    public async Task<(HarmonyResource resource, bool newResource)> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        var projectResourceCachePath = ProjectResourceCachePath;
        Directory.CreateDirectory(projectResourceCachePath);
        var localPath = Path.Combine(projectResourceCachePath, Path.GetFileName(metadata.Filename));
        if (File.Exists(localPath)) return ((await resourceService.AllResources()).First(r => r.LocalPath == localPath), newResource: false);
        //must scope just to the copy, otherwise we can't upload the file to the server
        await using (var localFile = File.Create(localPath))
        {
            await stream.CopyToAsync(localFile);
        }

        try
        {
            IRemoteResourceService? remoteResourceService = null;
            if (await httpClientProvider.ConnectionStatus() == ConnectionStatus.Online) remoteResourceService = this;
            return (await resourceService.AddLocalResource(
                localPath,
                currentProjectService.ProjectData.ClientId,
                resourceService: remoteResourceService
            ), newResource: true);
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
