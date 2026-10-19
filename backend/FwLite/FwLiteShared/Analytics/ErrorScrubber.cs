using System.Text;
using System.Text.RegularExpressions;

namespace FwLiteShared.Analytics;

/// <summary>
/// Strips likely user data from error text before it is sent to Mixpanel. Exception messages can carry
/// entry text, project names and file paths (which contain user names), so quoted values, paths, URLs and
/// emails are replaced with placeholders and every value is cut to Mixpanel's 255-byte string limit.
/// This is best effort: unquoted values interpolated into a message are not detectable.
/// </summary>
public static partial class ErrorScrubber
{
    /// <summary>Mixpanel truncates string property values to 255 bytes.</summary>
    public const int MaxStringBytes = 255;

    /// <summary>Mixpanel's limit for a list property is 8KB; stay a little under it.</summary>
    public const int MaxListBytes = 7500;

    private const int MaxStackFrames = 60;

    [GeneratedRegex(@"\b[a-zA-Z][a-zA-Z0-9+.\-]*://[^\s'""<>()]+")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"[\w.+\-]+@[\w\-]+(?:\.[\w\-]+)+")]
    private static partial Regex EmailRegex();

    // C:\foo\bar, C:/foo, \\server\share
    [GeneratedRegex(@"(?:\b[a-zA-Z]:[\\/]|\\\\)[^\s'""<>|]*")]
    private static partial Regex WindowsPathRegex();

    // /home/bob/x, ~/x; needs at least two segments so a lone "/" or "and/or" is left alone
    [GeneratedRegex(@"(?<![\w.:<])~?/(?:[^\s/'""<>]+/)+[^\s'""<>]*")]
    private static partial Regex UnixPathRegex();

    // A single quote inside a word is an apostrophe: "Can't open 'x'" must redact 'x', and 'Bob's entry' as a whole.
    // No backticks: .NET generic type names (List`1) use them.
    [GeneratedRegex(@"(?<!\w)'(?:[^'\r\n]|'(?=\w))*'(?!\w)|""[^""\r\n]*""|“[^”\r\n]*”|‘[^’\r\n]*’|«[^»\r\n]*»")]
    private static partial Regex QuotedRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    // .NET frame source location: " in C:\build\src\Foo.cs:line 12" -> " in Foo.cs:line 12"
    [GeneratedRegex(@" in (?:.*[\\/])?([^\\/]+?):line ")]
    private static partial Regex DotnetFrameSourceRegex();

    // JS frame location: "http://localhost:5173/src/x.ts?t=1:10:5" -> "/src/x.ts:10:5"
    [GeneratedRegex(@"\b[a-zA-Z][a-zA-Z0-9+.\-]*://[^/\s)]*")]
    private static partial Regex FrameOriginRegex();

    [GeneratedRegex(@"\?[^\s):]*")]
    private static partial Regex FrameQueryRegex();

    // "/Users/bob/..." or "\home\bob\..." -> "/Users/<user>/..."
    [GeneratedRegex(@"(?i)([\\/](?:Users|home|Documents and Settings)[\\/])[^\\/\s]+")]
    private static partial Regex UserDirRegex();

    /// <summary>Scrub a free-text message (exception message, formatted log message) and collapse it to one line.</summary>
    public static string? ScrubMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;
        var scrubbed = UrlRegex().Replace(message, "<url>");
        scrubbed = EmailRegex().Replace(scrubbed, "<email>");
        scrubbed = WindowsPathRegex().Replace(scrubbed, "<path>");
        scrubbed = UnixPathRegex().Replace(scrubbed, "<path>");
        scrubbed = QuotedRegex().Replace(scrubbed, "<redacted>");
        scrubbed = WhitespaceRegex().Replace(scrubbed, " ").Trim();
        return Truncate(scrubbed, MaxStringBytes);
    }

    /// <summary>
    /// Split a .NET or JS stack trace into scrubbed frames: source directories, URL origins and query strings
    /// are dropped, and the list is capped to fit a Mixpanel list property.
    /// </summary>
    public static List<string> ScrubStack(string? stackTrace)
    {
        var frames = new List<string>();
        if (string.IsNullOrWhiteSpace(stackTrace))
            return frames;
        var totalBytes = 0;
        foreach (var rawLine in stackTrace.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("--- End of", StringComparison.Ordinal))
                continue;
            line = DotnetFrameSourceRegex().Replace(line, " in $1:line ");
            line = FrameOriginRegex().Replace(line, "");
            line = FrameQueryRegex().Replace(line, "");
            line = UserDirRegex().Replace(line, "$1<user>");
            line = EmailRegex().Replace(line, "<email>");
            line = QuotedRegex().Replace(line, "<redacted>");
            line = Truncate(line, MaxStringBytes);
            // + quotes and comma in the JSON array
            totalBytes += Encoding.UTF8.GetByteCount(line) + 3;
            if (totalBytes > MaxListBytes || frames.Count >= MaxStackFrames)
                break;
            frames.Add(line);
        }
        return frames;
    }

    /// <summary>Cut <paramref name="value"/> to at most <paramref name="maxBytes"/> UTF-8 bytes without splitting a character.</summary>
    public static string Truncate(string value, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
            return value;
        const string ellipsis = "…";
        var budget = maxBytes - Encoding.UTF8.GetByteCount(ellipsis);
        var bytes = 0;
        var end = 0;
        while (end < value.Length)
        {
            var length = char.IsSurrogatePair(value, end) ? 2 : 1;
            var charBytes = Encoding.UTF8.GetByteCount(value.AsSpan(end, length));
            if (bytes + charBytes > budget)
                break;
            bytes += charBytes;
            end += length;
        }
        return value[..end] + ellipsis;
    }
}
