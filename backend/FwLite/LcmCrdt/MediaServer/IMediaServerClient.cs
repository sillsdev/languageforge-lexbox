using System.Text.Json.Serialization;
using MiniLcm.Media;
using Refit;

namespace LcmCrdt.MediaServer;


public interface IMediaServerClient
{
    [Get("/api/media/metadata/{fileId}")]
    Task<LcmFileMetadata> GetFileMetadata(Guid fileId);
    
    [Get("/api/media/{fileId}")]
    Task<HttpResponseMessage> DownloadFile(Guid fileId);

    [Post("/api/media")]
    [Multipart]
    Task<MediaUploadFileResponse> UploadFile(MultipartItem file,
        [Query] Guid projectId,
        string fileId,//using a string because Refit doesn't handle a Guid properly
        string? author = null,
        string? filename = null);
}

public record MediaUploadFileResponse(Guid Guid, FileMetadata? Metadata);

public class FileMetadata
{
    public string? Sha256Hash { get; set; }
    public int? SizeInBytes { get; set; }
    public string? FileFormat { get; set; }
    public string? MimeType { get; set; }
    public string? Author { get; set; }
    public DateTimeOffset? UploadDate { get; set; }

    // Other optional, not-yet-mapped properties like purpose, transcript, etc., should end up in here
    [JsonExtensionData] 
    public IDictionary<string, object> ExtraFields { get; set; } = new Dictionary<string, object>();
}