namespace LcmCrdt.MediaServer;

public static class MediaFileNames
{
    /// <summary>Prefix for a file still being copied; it is renamed once complete.</summary>
    public const string PartialPrefix = ".partial-";

    // illegal on Windows and on the FAT32/exFAT file systems USB drives use, even when this device is Linux
    private static readonly HashSet<char> Unsafe = [.. Path.GetInvalidFileNameChars(), '<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <summary>
    /// A file name every platform and USB drive accepts, since media copied by local sync can come from any of them.
    /// </summary>
    public static string MakeSafe(string fileName)
    {
        var safe = new string([.. fileName.Select(c => Unsafe.Contains(c) || char.IsControl(c) ? '_' : c)]).TrimEnd('.', ' ');
        return safe.Length == 0 || safe.StartsWith(PartialPrefix, StringComparison.Ordinal) ? "file" + safe : safe;
    }
}
