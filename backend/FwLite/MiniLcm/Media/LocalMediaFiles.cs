namespace MiniLcm.Media;

/// <summary>
/// Helpers for saving uploaded media into a folder keyed by filename, where a different file
/// with the same name (e.g. two phones' IMG_0001.jpg) must not be mistaken for the existing one.
/// </summary>
public static class LocalMediaFiles
{
    /// <summary>
    /// Copies the stream to a new file in the system temp folder. Uploads can't be read twice,
    /// and the copy is needed to compare against an existing file before choosing the final name.
    /// </summary>
    public static async Task<string> CopyToTempFile(Stream stream)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"fwlite-upload-{Guid.NewGuid():N}.tmp");
        try
        {
            await using var tempFile = File.Create(tempPath);
            await stream.CopyToAsync(tempFile);
        }
        catch
        {
            DeleteIfExists(tempPath);
            throw;
        }
        return tempPath;
    }

    public static bool ContentEquals(string pathA, string pathB)
    {
        var a = new FileInfo(pathA);
        var b = new FileInfo(pathB);
        if (!a.Exists || !b.Exists || a.Length != b.Length) return false;

        const int bufferSize = 64 * 1024;
        using var streamA = a.OpenRead();
        using var streamB = b.OpenRead();
        var bufferA = new byte[bufferSize];
        var bufferB = new byte[bufferSize];
        while (true)
        {
            var readA = streamA.ReadAtLeast(bufferA, bufferSize, throwOnEndOfStream: false);
            var readB = streamB.ReadAtLeast(bufferB, bufferSize, throwOnEndOfStream: false);
            if (readA != readB) return false;
            if (readA == 0) return true;
            if (!bufferA.AsSpan(0, readA).SequenceEqual(bufferB.AsSpan(0, readB))) return false;
        }
    }

    /// <summary>
    /// Moves <paramref name="sourcePath"/> to <paramref name="targetPath"/>, or to the first free
    /// "name-1.ext", "name-2.ext"… beside it. Never overwrites, even if another save claims the
    /// same name concurrently.
    /// </summary>
    /// <returns>The path the file was moved to.</returns>
    public static string MoveToFreePath(string sourcePath, string targetPath)
    {
        for (var counter = 0; ; counter++)
        {
            var candidate = counter == 0 ? targetPath : NumberedPath(targetPath, counter);
            if (File.Exists(candidate)) continue;
            try
            {
                File.Move(sourcePath, candidate, overwrite: false);
                return candidate;
            }
            catch (IOException) when (File.Exists(candidate))
            {
                // lost a race for this name, try the next one
            }
        }
    }

    /// <summary>
    /// Returns <paramref name="filePath"/>, or the first free "name-1.ext", "name-2.ext"… beside it.
    /// </summary>
    public static string EnsureUnique(string filePath)
    {
        if (!File.Exists(filePath)) return filePath;
        for (var counter = 1; ; counter++)
        {
            var candidate = NumberedPath(filePath, counter);
            if (!File.Exists(candidate)) return candidate;
        }
    }

    public static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static string NumberedPath(string filePath, int counter)
    {
        var directory = Path.GetDirectoryName(filePath);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(filePath)}-{counter}{Path.GetExtension(filePath)}");
    }
}
