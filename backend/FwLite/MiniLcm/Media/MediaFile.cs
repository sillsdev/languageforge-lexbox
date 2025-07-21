using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record MediaFile(MediaUri Uri, LcmFileMetadata Metadata, MediaFileType Type);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaFileType
{
    Other,
    Pdf,
    Text,
    Audio,
    Image,
    Video,
}
