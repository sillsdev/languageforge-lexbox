using System.Collections.Concurrent;
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
    // Coalesces concurrent downloads of the same file into one shared task. Without this, two
    // requests for a not-yet-cached file (e.g. the UI loading a picture more than once at first
    // render) both see no local resource and both call DownloadResource, whose AddLocalResource
    // inserts a LocalResource keyed by the file id — the second insert then fails with
    // "UNIQUE constraint failed: LocalResource.Id". The first caller starts the task; others
    // await the same one. Keyed by file id (a unique Guid), so different files still download in
    // parallel.
    private static readonly ConcurrentDictionary<Guid, Task<LocalResource>> DownloadTasks = new();

    public async Task<ReadFileResponse> GetFileStream(Guid fileId)
    {
        var localResource = await resourceService.GetLocalResource(fileId);
        if (localResource is null)
        {
            var connectionStatus = await httpClientProvider.ConnectionStatus();
            if (connectionStatus != ConnectionStatus.Online)
            {
                return new ReadFileResponse(ReadFileResult.Offline);
            }

            localResource = await GetOrStartDownload(fileId);
        }
        //todo, consider trying to download the file again, maybe the cache was cleared
        if (!File.Exists(localResource.LocalPath))
            throw new FileNotFoundException("Unable to find the file with Id" + fileId, localResource.LocalPath);
        return new(File.OpenRead(localResource.LocalPath), Path.GetFileName(localResource.LocalPath));
    }

    private Task<LocalResource> GetOrStartDownload(Guid fileId)
    {
        // Use the *value* overload of GetOrAdd, not the factory overload: the factory can run more
        // than once under contention (only one result is kept), which would start the download
        // twice. Passing a not-yet-started TaskCompletionSource.Task lets exactly one caller — the
        // one whose task actually got stored — kick off the download.
        var tcs = new TaskCompletionSource<LocalResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = DownloadTasks.GetOrAdd(fileId, tcs.Task);
        if (task != tcs.Task)
        {
            // Another caller already owns the in-flight download; await theirs (our tcs is unused).
            return task;
        }
        _ = RunDownloadAsync(fileId, tcs);
        return task;
    }

    private async Task RunDownloadAsync(Guid fileId, TaskCompletionSource<LocalResource> tcs)
    {
        try
        {
            // Re-check before downloading: a previous download may have committed the LocalResource
            // after our caller's GetLocalResource miss (this closes the race between
            // GetLocalResource and DownloadResource). The task entry is removed only after this
            // completes — i.e. after the download has committed — so any later caller that starts a
            // fresh task will find the committed resource here and skip the download, rather than
            // inserting a duplicate primary key.
            var resource = await resourceService.GetLocalResource(fileId)
                           ?? await resourceService.DownloadResource(fileId, this);
            tcs.SetResult(resource);
        }
        catch (Exception e)
        {
            tcs.SetException(e);
        }
        finally
        {
            // Cached now, so future misses can start fresh; also prevents a failed download from
            // being cached as a permanently-faulted task.
            DownloadTasks.TryRemove(fileId, out _);
        }
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
