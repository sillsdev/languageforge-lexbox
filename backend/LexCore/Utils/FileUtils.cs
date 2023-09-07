using System.Globalization;

namespace LexCore.Utils;

public static class FileUtils
{
    public static string ToTimestamp(DateTimeOffset dateTime)
    {
        var timestamp = dateTime.ToString(DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern);
        // make it file-system friendly
        return timestamp.Replace(':', '-');
    }
}
