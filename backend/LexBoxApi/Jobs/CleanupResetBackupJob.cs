using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using Quartz;

namespace LexBoxApi.Jobs;

public class CleanupResetBackupJob(IHgService hgService, ILogger<CleanupResetBackupJob> logger) : LexJob
{
    public static JobKey Key { get; } = new("CleanupResetBackupJob");

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        logger.LogInformation("Starting cleanup reset backup job");

        await Task.Delay(TimeSpan.FromSeconds(1));

        logger.LogInformation("Finished cleanup reset backup job");
    }
}
