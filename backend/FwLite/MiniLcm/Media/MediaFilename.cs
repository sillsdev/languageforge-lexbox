using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace MiniLcm.Media;

/// <summary>
/// Media file names must be storable on Windows wherever they're created, because FLEx users receive the
/// files via Send/Receive. The rules are hardcoded: Path.GetInvalidFileNameChars() only returns the host
/// OS's rules ('/' and '\0' on Linux).
/// </summary>
public static class MediaFilename
{
    private static readonly SearchValues<char> InvalidChars =
        SearchValues.Create("\"*/:<>?\\|" + string.Concat(Enumerable.Range(0, 32).Select(i => (char)i)));

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static bool IsValid([NotNullWhen(true)] string? filename)
    {
        return !string.IsNullOrEmpty(filename)
            && !filename.AsSpan().ContainsAny(InvalidChars)
            && !filename.EndsWith('.') && !filename.EndsWith(' ')
            && !ReservedNames.Contains(filename.Split('.')[0]);
    }
}
