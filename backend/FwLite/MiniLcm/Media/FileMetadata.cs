using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record LcmFileMetadata(
    string Filename,
    string MimeType,
    string? Author = null,
    DateTimeOffset? UploadDate = null,
    long? SizeInBytes = null,
    // LinkedFiles subfolder for the server-side copy (e.g. "Plugins"); one path segment, chosen by the app, never by user input.
    // Omitted when null so existing media metadata serializes unchanged.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? LinkedFilesSubfolder = null)
{
    // Other optional metadata can end up here, eg duration
    [JsonExtensionData] 
    public IDictionary<string, object> ExtraFields { get; set; } = new Dictionary<string, object>();
}
