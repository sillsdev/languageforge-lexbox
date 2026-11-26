using System.Collections.Concurrent;
using System.Diagnostics;

namespace FwLiteShared.Services;

/// <summary>
/// Startup and new-window milestones in ms since the process started, logged when a window's viewer has loaded.
/// </summary>
public static class StartupTiming
{
    private static readonly DateTime ProcessStart = GetProcessStart();
    private static readonly ConcurrentQueue<string> Marks = new();

    public static void Mark(string milestone) =>
        Marks.Enqueue($"{milestone} {(long)(DateTime.UtcNow - ProcessStart).TotalMilliseconds}");

    /// <summary>The marks since the last call.</summary>
    public static string TakeMarks()
    {
        List<string> marks = [];
        while (Marks.TryDequeue(out var mark)) marks.Add(mark);
        return string.Join(", ", marks);
    }

    private static DateTime GetProcessStart()
    {
        try
        {
            return Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }
        catch (Exception)
        {
            //not available on every platform, the marks are then relative to the first one
            return DateTime.UtcNow;
        }
    }
}
