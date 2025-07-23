using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record MediaFile(MediaUri Uri, LcmFileMetadata Metadata)
{
    public const int MaxFileSize = 10 * 1024 * 1024; // 10MB
}
