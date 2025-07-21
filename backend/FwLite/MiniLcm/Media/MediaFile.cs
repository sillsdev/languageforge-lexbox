using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record MediaFile(MediaUri Uri, LcmFileMetadata Metadata);
