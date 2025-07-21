namespace MiniLcm.Media;

public record LcmFileMetadata(
    string Filename,
    string? MimeType = null,
    string? Author = null,
    DateTimeOffset? UploadDate = null);
