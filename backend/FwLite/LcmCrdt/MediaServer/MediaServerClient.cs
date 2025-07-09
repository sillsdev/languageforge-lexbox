using MiniLcm.Project;
using Refit;

namespace LcmCrdt.MediaServer;

public class MediaServerClient(RefitSettings refitSettings, IServerHttpClientProvider httpClientProvider)
{
    public async Task<Stream?> GetFileStream(Guid fileId)
    {
        var httpClient = await httpClientProvider.GetHttpClient();
        var response = await RestService.For<IMediaServerClient>(httpClient, refitSettings).DownloadFile(fileId);
        return await response.Content.ReadAsStreamAsync();
    }
}

public interface IMediaServerClient
{
    [Get("/api/media/{fileId}")]
    Task<HttpResponseMessage> DownloadFile(Guid fileId);
}
