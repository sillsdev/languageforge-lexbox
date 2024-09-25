using System.Globalization;
using System.Runtime.InteropServices;

namespace LexCore.Utils;

public static class FileUtils
{
    private static readonly string TimestampPattern = DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern.Replace(':', '-');
    public static string ToTimestamp(DateTimeOffset dateTime)
    {
        var timestamp = dateTime.ToUniversalTime().ToString(TimestampPattern);
        // make it file-system friendly
        return timestamp.Replace(':', '-');
    }

    public static DateTimeOffset? ToDateTimeOffset(string timestamp)
    {
        if (DateTimeOffset.TryParseExact(timestamp, TimestampPattern, null, DateTimeStyles.AssumeUniversal, out var dateTime))
        {
            return dateTime;
        }

        return null;
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
