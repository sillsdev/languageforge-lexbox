using System.Collections.Concurrent;
using System.Net;
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
            try
            {
                localResource = await GetOrStartDownload(fileId);
            }
            catch
            {
                // Did the download fail because we're offline? Return an explicit "we're offline" result
                // so that the frontend can display some appropriate UI. If we're online and yet the download
                // failed, then that's a real error that should be rethrown.
                if (await httpClientProvider.ConnectionStatus() == ConnectionStatus.Offline)
                {
                    return new ReadFileResponse(ReadFileResult.Offline);
                }
                throw;
            }
        }
        //todo, consider trying to download the file again, maybe the cache was cleared
        if (!File.Exists(localResource.LocalPath))
            throw new FileNotFoundException("Unable to find the file with Id" + fileId, localResource.LocalPath);
        return new(File.OpenRead(localResource.LocalPath), Path.GetFileName(localResource.LocalPath));
    }

    // Single-flight download starter. Uses the *value* overload of GetOrAdd, not the factory
    // overload: ConcurrentDictionary may invoke a factory more than once under contention, and an
    // async factory with side effects would start a separate DownloadResource on each invocation —
    // the losing tasks still run and race to AddLocalResource, reintroducing the duplicate-key
    // insert. Handing GetOrAdd a not-yet-started TaskCompletionSource.Task means only the caller
    // whose task actually got stored kicks off the download; everyone else awaits that same task.
    private async Task<LocalResource> GetOrStartDownload(Guid fileId)
    {
        var tcs = new TaskCompletionSource<LocalResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = DownloadTasks.GetOrAdd(fileId, tcs.Task);
        if (task != tcs.Task) return await task;
        try
        {
            // Check again if we have it locally in case another thread committed it before we started.
            var resource = await resourceService.GetLocalResource(fileId)
                        ?? await resourceService.DownloadResource(fileId, this);
            tcs.SetResult(resource);
            return resource;
        }
        catch (Exception e)
        {
            tcs.SetException(e);
            throw;
        }
        finally
        {
            // Once the download completes, we need to remove the task from the ConcurrentDictionary
            // so that a new download can be attempted later by a different thread. That way if the local
            // file is ever deleted (say, by being replaced with a different picture) a new download can start.
            DownloadTasks.TryRemove(fileId, out _);
        }
    }

    // Media files are proxied (lexbox -> FwHeadless). A cold request — e.g. the first one after the
    // backend starts, before its code paths are JIT-warmed — can exceed the proxy's timeout and come
    // back as 504 (or 502/503) even though the backend keeps working and warms up. Retrying a
    // transient failure a few times usually hits the now-warm backend and succeeds. A failed fetch
    // never reaches AddLocalResource, so retrying cannot reintroduce the duplicate-key insert.
    private const int MaxDownloadAttempts = 3;
    private static readonly HashSet<HttpStatusCode> TransientDownloadStatusCodes =
    [
        HttpStatusCode.RequestTimeout,      // 408
        HttpStatusCode.BadGateway,          // 502
        HttpStatusCode.ServiceUnavailable,  // 503
        HttpStatusCode.GatewayTimeout,      // 504
        (HttpStatusCode)520, // Non-standard status code used by Cloudflare for "Server returned unknown error", seen in CI
    ];

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
        var mediaClient = await MediaServerClient();
        var fileId = new Guid(remoteId);
        for (var attempt = 1; ; attempt++)
        {
            using var response = await mediaClient.DownloadFile(fileId);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                if (stream is null) throw new FileNotFoundException(remoteId);
                var filename = response.Content.Headers.ContentDisposition?.FileName?.Replace("\"", "");
                filename = Path.GetFileName(filename);
                var localPath = Path.Combine(projectResourceCachePath, filename ?? remoteId);
                localPath = EnsureUnique(localPath);
                await using var localFile = File.Create(localPath);
                await stream.CopyToAsync(localFile);
                return new DownloadResult(localPath);
            }

            var statusCode = response.StatusCode;
            var reasonPhrase = response.ReasonPhrase;
            if (attempt >= MaxDownloadAttempts || !TransientDownloadStatusCodes.Contains(statusCode))
            {
                throw new Exception($"Failed to download file {fileId}: {statusCode} {reasonPhrase}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }
    }

    public string ProjectResourceCachePath => ProjectCachePath(currentProjectService.Project, options.Value);

    public static string ProjectCachePath(CrdtProject project, CrdtConfig options)
    {
        return Path.Combine(options.LocalResourceCachePath, project.Name);
    }


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
