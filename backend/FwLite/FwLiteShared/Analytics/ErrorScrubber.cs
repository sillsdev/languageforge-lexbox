using System.Text;
using System.Text.RegularExpressions;

namespace FwLiteShared.Analytics;

/// <summary>
/// Strips likely user data from error text before it is sent to Mixpanel. Exception messages can carry
/// entry text, project names and file paths (which contain user names), so quoted values, paths, URLs and
/// emails are replaced with placeholders and every value is cut to Mixpanel's 255-byte string limit.
/// This is best effort: unquoted values interpolated into a message are not detectable.
/// </summary>
/// <remarks>
/// Each input is scanned once with <see cref="Regex.EnumerateMatches(ReadOnlySpan{char})"/> and written into a
/// stack buffer, so the only allocation is the returned string. The output is capped at 255 bytes, so the
/// buffer never needs to grow: anything past it is truncated anyway.
/// </remarks>
public static partial class ErrorScrubber
{
    /// <summary>Mixpanel truncates string property values to 255 bytes.</summary>
    public const int MaxStringBytes = 255;

    /// <summary>Mixpanel's limit for a list property is 8KB; stay a little under it.</summary>
    public const int MaxListBytes = 7500;

    private const int MaxStackFrames = 60;
    private const string Ellipsis = "…";

    // One alternation so each input is scanned once; at any position the earliest alternative wins.
    // EnumerateMatches doesn't expose groups, so MessagePlaceholder tells the alternatives apart by their first chars.
    [GeneratedRegex(
        // URL
        @"\b[a-zA-Z][a-zA-Z0-9+.\-]*://[^\s'""<>()]+" +
        // email
        @"|[\w.+\-]+@[\w\-]+(?:\.[\w\-]+)+" +
        // C:\foo\bar, C:/foo, \\server\share
        @"|(?:\b[a-zA-Z]:[\\/]|\\\\)[^\s'""<>|]*" +
        // /home/bob/x, ~/x; needs at least two segments so a lone "/" or "and/or" is left alone
        @"|(?<![\w.:])~?/(?:[^\s/'""<>]+/)+[^\s'""<>]*" +
        // quoted. A single quote inside a word is an apostrophe: "Can't open 'x'" must redact 'x', and
        // 'Bob's entry' as a whole. No backticks: .NET generic type names (List`1) use them.
        @"|(?<!\w)'(?:[^'\r\n]|'(?=\w))*'(?!\w)|""[^""\r\n]*""|“[^”\r\n]*”|‘[^’\r\n]*’|«[^»\r\n]*»")]
    private static partial Regex MessageRegex();

    // Same scheme for one stack frame; FramePlaceholder tells the alternatives apart.
    [GeneratedRegex(
        // .NET frame source location: " in C:\build\src\Foo.cs:line 12" -> " in Foo.cs:line 12"
        @" in (?:.*[\\/])?[^\\/]+?:line " +
        // JS frame URL origin: "http://localhost:5173/src/x.ts:10:5" -> "/src/x.ts:10:5"
        @"|\b[a-zA-Z][a-zA-Z0-9+.\-]*://[^/\s)]*" +
        // query string: "/src/x.ts?t=1:10:5" -> "/src/x.ts:10:5"
        @"|\?[^\s):]*" +
        // "/Users/bob" or "\home\bob" -> "/Users/<user>"
        @"|(?i:[\\/](?:Users|home|Documents and Settings)[\\/])[^\\/\s]+" +
        // email
        @"|[\w.+\-]+@[\w\-]+(?:\.[\w\-]+)+" +
        // quoted, as in MessageRegex
        @"|(?<!\w)'(?:[^'\r\n]|'(?=\w))*'(?!\w)|""[^""\r\n]*""|“[^”\r\n]*”|‘[^’\r\n]*’|«[^»\r\n]*»")]
    private static partial Regex FrameRegex();

    /// <summary>Scrub a free-text message (exception message, formatted log message) and collapse it to one line.</summary>
    public static string? ScrubMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;
        var writer = new BoundedWriter(stackalloc char[MaxStringBytes + 1]);
        var input = message.AsSpan();
        var last = 0;
        foreach (var match in MessageRegex().EnumerateMatches(input))
        {
            writer.AppendCollapsingWhitespace(input[last..match.Index]);
            writer.Append(MessagePlaceholder(input.Slice(match.Index, match.Length)));
            last = match.Index + match.Length;
            if (writer.Overflowed)
                break;
        }
        writer.AppendCollapsingWhitespace(input[last..]);
        return writer.ToTruncatedString(MaxStringBytes);
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
        Span<char> buffer = stackalloc char[MaxStringBytes + 1];
        var totalBytes = 0;
        foreach (var rawLine in stackTrace.AsSpan().EnumerateLines())
        {
            var line = rawLine.Trim();
            if (line.IsEmpty || line.StartsWith("--- End of", StringComparison.Ordinal))
                continue;
            var frame = ScrubFrame(line, buffer);
            // + quotes and comma in the JSON array
            totalBytes += Encoding.UTF8.GetByteCount(frame) + 3;
            if (totalBytes > MaxListBytes || frames.Count >= MaxStackFrames)
                break;
            frames.Add(frame);
        }
        return frames;
    }

    /// <summary>Cut <paramref name="value"/> to at most <paramref name="maxBytes"/> UTF-8 bytes without splitting a character.</summary>
    public static string Truncate(string value, int maxBytes)
    {
        return Encoding.UTF8.GetByteCount(value) <= maxBytes ? value : TruncateWithEllipsis(value, maxBytes);
    }

    private static string ScrubFrame(ReadOnlySpan<char> line, Span<char> buffer)
    {
        var writer = new BoundedWriter(buffer);
        var last = 0;
        foreach (var match in FrameRegex().EnumerateMatches(line))
        {
            writer.Append(line[last..match.Index]);
            AppendFramePlaceholder(ref writer, line.Slice(match.Index, match.Length));
            last = match.Index + match.Length;
            if (writer.Overflowed)
                break;
        }
        writer.Append(line[last..]);
        return writer.ToTruncatedString(MaxStringBytes);
    }

    private static ReadOnlySpan<char> MessagePlaceholder(ReadOnlySpan<char> match)
    {
        if (IsQuote(match[0]))
            return "<redacted>";
        if (match.Contains("://", StringComparison.Ordinal))
            return "<url>";
        // an email can't start with a separator or contain ':', so these are paths
        if (match[0] is '/' or '\\' or '~' || (match.Length > 1 && match[1] == ':'))
            return "<path>";
        return "<email>";
    }

    private static void AppendFramePlaceholder(ref BoundedWriter writer, ReadOnlySpan<char> match)
    {
        if (match.StartsWith(" in ", StringComparison.Ordinal))
        {
            // " in <dir>/<file>:line " -> " in <file>:line "
            var path = match[" in ".Length..^":line ".Length];
            writer.Append(" in ");
            writer.Append(path[(path.LastIndexOfAny('/', '\\') + 1)..]);
            writer.Append(":line ");
        }
        else if (match[0] == '?' || match.Contains("://", StringComparison.Ordinal))
        {
            // drop URL origins and query strings
        }
        else if (match[0] is '/' or '\\')
        {
            // keep "/Users/", replace the user name
            var userStart = match[1..].IndexOfAny('/', '\\') + 2;
            writer.Append(match[..userStart]);
            writer.Append("<user>");
        }
        else
        {
            writer.Append(IsQuote(match[0]) ? "<redacted>" : "<email>");
        }
    }

    private static bool IsQuote(char c) => c is '\'' or '"' or '“' or '‘' or '«';

    private static string TruncateWithEllipsis(ReadOnlySpan<char> value, int maxBytes)
    {
        var budget = maxBytes - Encoding.UTF8.GetByteCount(Ellipsis);
        var bytes = 0;
        var end = 0;
        while (end < value.Length)
        {
            Rune.DecodeFromUtf16(value[end..], out var rune, out var charsConsumed);
            if (bytes + rune.Utf8SequenceLength > budget)
                break;
            bytes += rune.Utf8SequenceLength;
            end += charsConsumed;
        }
        return string.Concat(value[..end], Ellipsis);
    }

    /// <summary>Writes into a fixed buffer, remembering whether anything didn't fit.</summary>
    private ref struct BoundedWriter(Span<char> buffer)
    {
        private readonly Span<char> _buffer = buffer;
        private int _length;

        public bool Overflowed { get; private set; }

        public void Append(ReadOnlySpan<char> value)
        {
            var fits = Math.Min(value.Length, _buffer.Length - _length);
            value[..fits].CopyTo(_buffer[_length..]);
            _length += fits;
            if (fits < value.Length)
                Overflowed = true;
        }

        private void Append(char c)
        {
            if (_length < _buffer.Length)
                _buffer[_length++] = c;
            else
                Overflowed = true;
        }

        /// <summary>Append with every whitespace run (newlines included) collapsed to one space and no leading space.</summary>
        public void AppendCollapsingWhitespace(ReadOnlySpan<char> value)
        {
            foreach (var c in value)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (_length > 0 && _buffer[_length - 1] != ' ')
                        Append(' ');
                }
                else
                {
                    Append(c);
                }
                if (Overflowed)
                    return;
            }
        }

        public readonly string ToTruncatedString(int maxBytes)
        {
            var written = _buffer[.._length].TrimEnd(' ');
            if (!Overflowed && Encoding.UTF8.GetByteCount(written) <= maxBytes)
                return new string(written);
            return TruncateWithEllipsis(written, maxBytes);
        }
    }
}
