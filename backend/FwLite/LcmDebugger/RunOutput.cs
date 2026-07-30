using System.Runtime.CompilerServices;

namespace LcmDebugger;

/// <summary>
/// Names the files a run leaves behind, in LcmDebugger/logs (gitignored, never pruned).
/// One timestamp per process, so a run's log and its dry run records sort together.
/// </summary>
public static class RunOutput
{
    private static readonly string RunStarted = $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}";
    private static readonly string LogDir = GetLogDir();

    public static string FilePath(string name)
    {
        Directory.CreateDirectory(LogDir);
        return Path.Combine(LogDir, $"{RunStarted}-{name}");
    }

    private static string GetLogDir([CallerFilePath] string thisFilePath = "")
    {
        var sourceDir = Path.GetDirectoryName(thisFilePath) ??
            throw new InvalidOperationException("Could not determine source file directory");
        return Path.Combine(sourceDir, "logs");
    }
}
