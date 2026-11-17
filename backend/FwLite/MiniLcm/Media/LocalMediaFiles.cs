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
    /// The files among <paramref name="targetPath"/> and its taken "name-1.ext", "name-2.ext"…
    /// whose content equals <paramref name="sourcePath"/>, i.e. earlier saves of the same file.
    /// </summary>
    public static IEnumerable<string> ExistingCopies(string sourcePath, string targetPath)
    {
        return Candidates(targetPath).TakeWhile(File.Exists).Where(candidate => ContentEquals(sourcePath, candidate));
    }

    /// <summary>
    /// Moves <paramref name="sourcePath"/> to <paramref name="targetPath"/>, or to the first free
    /// "name-1.ext", "name-2.ext"… beside it. Never overwrites, even if another save claims the
    /// same name concurrently.
    /// </summary>
    /// <returns>The path the file was moved to.</returns>
    public static string MoveToFreePath(string sourcePath, string targetPath)
    {
        return Candidates(targetPath).First(candidate => TryMove(sourcePath, candidate));
    }

    private static bool TryMove(string sourcePath, string targetPath)
    {
        if (File.Exists(targetPath)) return false;
        try
        {
            File.Move(sourcePath, targetPath, overwrite: false);
            return true;
        }
        catch (IOException) when (File.Exists(targetPath))
        {
            // lost a race for this name
            return false;
        }
    }

    /// <summary>
    /// Returns <paramref name="filePath"/>, or the first free "name-1.ext", "name-2.ext"… beside it.
    /// </summary>
    public static string EnsureUnique(string filePath)
    {
        return Candidates(filePath).First(candidate => !File.Exists(candidate));
    }

    public static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static IEnumerable<string> Candidates(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        var name = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        return Enumerate();

        IEnumerable<string> Enumerate()
        {
            yield return filePath;
            for (var counter = 1; ; counter++)
            {
                yield return Path.Combine(directory, $"{name}-{counter}{extension}");
            }
        }
    }
}
