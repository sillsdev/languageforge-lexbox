using Quartz;

namespace LexBoxApi.Jobs;

public class DeleteTempDirectoryJob() : DelayedLexJob
{
    public static async Task Queue(ISchedulerFactory schedulerFactory,
        string path,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
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
        return Task.Run(() =>
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        });
    }
}
