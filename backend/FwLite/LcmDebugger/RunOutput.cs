using System.Runtime.CompilerServices;

namespace LcmDebugger;

/// <summary>
/// Names the files a run leaves behind, in LcmDebugger/logs (gitignored, never pruned).
/// One timestamp per process, so a run's log and its dry run records sort together.
/// </summary>
public static class RunOutput
{
    // milliseconds so two runs started in the same second don't share a name
    private static readonly string RunStarted = $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss-fff}";
    private static readonly string LogDir = GetLogDir();

    public static string FilePath(string name)
    {
        Directory.CreateDirectory(LogDir);
        return Path.Combine(LogDir, $"{RunStarted}-{AsFileName(name)}");
    }

    // callers name files after a project, whose path can be nested
    private static string AsFileName(string name) =>
        string.Join('-', name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

    private static string GetLogDir([CallerFilePath] string thisFilePath = "")
    {
        var sourceDir = Path.GetDirectoryName(thisFilePath) ??
            throw new InvalidOperationException("Could not determine source file directory");
        return Path.Combine(sourceDir, "logs");
    }
}
