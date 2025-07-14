using Microsoft.Extensions.Options;
using MiniLcm.Project;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;

namespace LcmCrdt.MediaServer;

public class MediaServerClient(
    RefitSettings refitSettings,
    IServerHttpClientProvider httpClientProvider,
    ResourceService resourceService,
    CurrentProjectService currentProjectService,
    IOptions<CrdtConfig> options): IRemoteResourceService
{
    public async Task<Stream?> GetFileStream(Guid fileId)
    {
        var localResource = await resourceService.GetLocalResource(fileId)
                            ?? await resourceService.DownloadResource(fileId, this);
        if (!File.Exists(localResource.LocalPath))//todo, consider trying to download the file again, maybe the cache was cleared
            throw new FileNotFoundException("Unable to find the file with Id" + fileId, localResource.LocalPath);
        return File.OpenRead(localResource.LocalPath);
    }

    private async Task<(Stream? stream, string? filename)> StreamMediaFile(Guid fileId)
    {
        var httpClient = await httpClientProvider.GetHttpClient();
        var response = await RestService.For<IMediaServerClient>(httpClient, refitSettings).DownloadFile(fileId);
        return (await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentDisposition?.FileName);
    }

    async Task<DownloadResult> IRemoteResourceService.DownloadResource(string remoteId, string localResourceCachePath)
    {
        var projectResourceCachePath = ProjectResourceCachePath;
        Directory.CreateDirectory(projectResourceCachePath);
        var (stream, filename) = await StreamMediaFile(new Guid(remoteId));
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

    public string ProjectResourceCachePath => Path.Combine(options.Value.LocalResourceCachePath, currentProjectService.Project.Name);

    public Task<UploadResult> UploadResource(Guid resourceId, string localPath)
    {
        throw new NotImplementedException();
    }
}

public interface IMediaServerClient
{
    [Get("/api/media/{fileId}")]
    Task<HttpResponseMessage> DownloadFile(Guid fileId);
}
