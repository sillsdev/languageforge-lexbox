using MiniLcm.Media;
using Refit;

namespace LcmCrdt.MediaServer;


public interface IMediaServerClient
{
    [Get("/api/media/{fileId}")]
    Task<HttpResponseMessage> DownloadFile(Guid fileId);

    [Get("/api/media/metadata/{fileId}")]
    Task<LcmFileMetadata> GetFileMetadata(Guid fileId);

    [Post("/api/media")]
    [Multipart]
    Task<MediaUploadFileResponse> UploadFile(MultipartItem file,
        [Query] Guid projectId,
        string fileId,//using a string because Refit doesn't handle a Guid properly
        string? author = null,
        string? filename = null);
}

public record MediaUploadFileResponse(Guid Guid);
