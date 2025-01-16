using Quartz;

namespace LexBoxApi.Jobs;

public class DeleteTempDirectoryJob() : LexJob
{
    public static async Task Queue(ISchedulerFactory schedulerFactory,
        string path,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        if (!PathIsInTempDir(path)) return;
        await QueueJob(schedulerFactory,
            Key,
            new JobDataMap { { nameof(Path), path } },
            delay,
            cancellationToken);
    }

    public static JobKey Key { get; } = new(nameof(DeleteTempDirectoryJob), "CleanupJobs");
    public string? Path { get; set; }

    protected override Task ExecuteJob(IJobExecutionContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(Path);
        if (!PathIsInTempDir(Path)) return Task.CompletedTask;
        if (Directory.Exists(Path) && PathIsSafeToDelete(Path)) Directory.Delete(Path, true);
        return Task.CompletedTask;
    }

    private static bool PathIsInTempDir(string path)
    {
        // Only safe to delete files from the system temp directory
        var prefix = System.IO.Path.GetTempPath();
        return (!string.IsNullOrEmpty(prefix)) && path.StartsWith(prefix);
    }

    private static bool PathIsSafeToDelete(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            // Must be a directory *and* must not be a symlink
            return attributes.HasFlag(FileAttributes.Directory) && !attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false; // If anything at all goes wrong, we want to abort
        }
    }
}
