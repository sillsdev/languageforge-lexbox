using System.Globalization;
using System.Runtime.InteropServices;

namespace LexCore.Utils;

public static class FileUtils
{
    public static string ToTimestamp(DateTimeOffset dateTime)
    {
        var timestamp = dateTime.ToString(DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern);
        // make it file-system friendly
        return timestamp.Replace(':', '-');
    }

    public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target, UnixFileMode? permissions = null)
    {
        if (permissions.HasValue && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            target.UnixFileMode = permissions.Value;
        foreach (var dir in source.EnumerateDirectories())
        {
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name), permissions);
        }

        foreach (var file in source.EnumerateFiles())
        {
            var destFileName = Path.Combine(target.FullName, file.Name);
            var destFile = file.CopyTo(destFileName);
            if (permissions.HasValue && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                destFile.UnixFileMode = permissions.Value;
        }
    }
}
