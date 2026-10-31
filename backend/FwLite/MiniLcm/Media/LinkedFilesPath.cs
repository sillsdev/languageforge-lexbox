using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace MiniLcm.Media;

/// <summary>
/// Rules for the parts of a LinkedFiles path that clients choose. Shared by FW Lite (which picks the
/// subfolder when saving a file) and FwHeadless (which places the upload), so both sides agree.
/// </summary>
public static class LinkedFilesPath
{
    private static readonly SearchValues<char> SubfolderChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_");

    /// <summary>A subfolder is one plain path segment ("Pictures", "Plugins"); anything else could leave the project folder.</summary>
    public static bool IsValidSubfolder([NotNullWhen(true)] string? subfolder)
    {
        return !string.IsNullOrEmpty(subfolder) && !subfolder.AsSpan().ContainsAnyExcept(SubfolderChars);
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
