using System.Buffers;

namespace FwHeadless.Media;

/// <summary>
/// <see cref="LexCore.Entities.MediaFile.Filename"/> is a repo-relative path that is matched against
/// other paths by exact string comparison, so it must be canonicalised to a single separator convention
/// regardless of the OS that produced it (fw-headless runs on Linux '/', dev/test on Windows '\').
/// Otherwise a row stored under one convention fails to match its on-disk file and its Id — which anchors
/// the file in the CRDT/FwData bridge — gets churned into a fresh Guid.
/// </summary>
public static class MediaFilePath
{
    private static readonly SearchValues<char> SubfolderChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_");

    public static string Normalize(string path) => path.Replace('\\', '/');

    /// <summary>A LinkedFiles subfolder is one plain path segment ("Pictures"); anything else could leave the project folder.</summary>
    public static bool IsValidSubfolder(string subfolder)
    {
        return subfolder.Length > 0 && !subfolder.AsSpan().ContainsAnyExcept(SubfolderChars);
    }

    /// <summary>A bare file name: no directory part, not "." or "..", no control characters.</summary>
    public static bool IsValidFilename(string filename)
    {
        return filename.Length > 0
            && filename != "." && filename != ".."
            && filename.AsSpan().IndexOfAny('/', '\\') < 0
            && !filename.Any(char.IsControl)
            && Path.GetFileName(filename) == filename;
    }
}
