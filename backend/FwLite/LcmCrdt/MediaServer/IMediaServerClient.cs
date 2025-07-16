using Refit;

namespace LcmCrdt.MediaServer;


public interface IMediaServerClient
{
    [Get("/api/media/{fileId}")]
    Task<HttpResponseMessage> DownloadFile(Guid fileId);
}
