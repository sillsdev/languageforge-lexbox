using LexBoxApi.Services;
using Quartz;

namespace LexBoxApi.Jobs;

public class UpdateProjectMetadataJob(ProjectService projectService) : LexJob
{
    public static async Task Queue(ISchedulerFactory schedulerFactory,
        string projectCode,
        CancellationToken cancellationToken = default)
    {
        await QueueJob(schedulerFactory,
            Key,
            new JobDataMap { { nameof(ProjectCode), projectCode } },
            cancellationToken);
    }

    public static JobKey Key { get; } = new("UpdateProjectMetadataJob", "DataUpdate");
    public string? ProjectCode { get; set; }

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(ProjectCode);
        await projectService.UpdateProjectMetadata(ProjectCode);
    }
}
