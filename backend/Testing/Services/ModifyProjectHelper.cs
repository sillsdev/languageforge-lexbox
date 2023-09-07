using Mono.Unix.Native;

namespace Testing.Services;

public class ModifyProjectHelper
{
    public static void ModifyProject(string projectFilePath)
    {
        using var fileStream = File.Open(projectFilePath, FileMode.Open, FileAccess.ReadWrite);
        var position = FindPosition(fileStream, "DateModified val=\""u8);
        var timestampLength = "2023-08-16 09:28:29.436"u8.Length;
        Span<byte> span = stackalloc byte[timestampLength];
        if (fileStream.Read(span) != timestampLength)
        {
            throw new Exception("unable to read data");
        }

        // Encoding.UTF8.GetString(span).ShouldBe("2023-08-16 09:28:29.436");
        span[3] = span[3] == "2"u8[0] ? "3"u8[0] : "2"u8[0];

        fileStream.Position -= timestampLength;
        fileStream.Write(span);
        fileStream.Flush(true);

    }

    private static long FindPosition(Stream stream, ReadOnlySpan<byte> pattern)
    {
        int b;
        int i = 0;
        while ((b = stream.ReadByte()) != -1)
        {
            if (b == pattern[i])
            {
                if (++i == pattern.Length)
                {
                    return stream.Position - pattern.Length;
                }
            }
            else
            {
                i = 0;
            }
        }

        return -1;
    }
}
