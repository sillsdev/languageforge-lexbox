using System.Buffers;
using System.Diagnostics.CodeAnalysis;

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
    public static string Normalize(string path) => path.Replace('\\', '/');

    // Windows' rules, hardcoded: FLEx users receive these files via Send/Receive, and
    // Path.GetInvalidFileNameChars() only returns the host OS's rules ('/' and '\0' on Linux).
    private static readonly SearchValues<char> InvalidFilenameChars =
        SearchValues.Create("\"*/:<>?\\|" + string.Concat(Enumerable.Range(0, 32).Select(i => (char)i)));

    private static readonly HashSet<string> ReservedFilenames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>A bare file name that Windows can store.</summary>
    public static bool IsValidFilename([NotNullWhen(true)] string? filename)
    {
        return !string.IsNullOrEmpty(filename)
            && !filename.AsSpan().ContainsAny(InvalidFilenameChars)
            && !filename.EndsWith('.') && !filename.EndsWith(' ')
            && !ReservedFilenames.Contains(filename.Split('.')[0]);
    }
}
