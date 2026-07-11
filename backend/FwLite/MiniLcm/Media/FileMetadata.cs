namespace MiniLcm.Media;

public record LcmFileMetadata(
    string Filename,
    string MimeType,
    string? Author = null,
    DateTimeOffset? UploadDate = null,
    // LinkedFiles subfolder for the server-side copy (e.g. "Plugins"); one path segment, chosen by the app, never by user input.
    string? LinkedFilesSubfolder = null);
