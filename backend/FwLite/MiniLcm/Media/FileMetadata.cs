using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record LcmFileMetadata(
    string Filename,
    string MimeType,
    string? Author = null,
    DateTimeOffset? UploadDate = null,
    long? SizeInBytes = null)
{
    // Other optional metadata can end up here, eg duration
    [JsonExtensionData] 
    public IDictionary<string, object> ExtraFields { get; set; } = new Dictionary<string, object>();
}
