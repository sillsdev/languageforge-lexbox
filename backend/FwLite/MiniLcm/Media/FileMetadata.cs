namespace MiniLcm.Media;

public record LcmFileMetadata(
    string Filename,
    string MimeType,
    string? Author = null,
    DateTimeOffset? UploadDate = null);
