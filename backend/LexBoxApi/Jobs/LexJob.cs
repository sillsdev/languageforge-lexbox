using System.Diagnostics;
using LexBoxApi.Otel;
using OpenTelemetry.Trace;
using Quartz;

namespace LexBoxApi.Jobs;

public abstract class LexJob : IJob
{
    public string? JobTriggerTraceId { get; set; }
    public string? JobTriggerSpanParentId { get; set; }
    private bool JobTriggerWasTraced => !string.IsNullOrEmpty(JobTriggerTraceId) && !string.IsNullOrEmpty(JobTriggerSpanParentId);

    protected static async Task QueueJob(ISchedulerFactory schedulerFactory,
        JobKey key,
        JobDataMap data,
        CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        data[nameof(JobTriggerTraceId)] = Activity.Current?.Context.TraceId.ToHexString() ?? string.Empty;
        data[nameof(JobTriggerSpanParentId)] = Activity.Current?.Context.SpanId.ToHexString() ?? string.Empty;
        await scheduler.TriggerJob(key, data, cancellationToken);
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        using var activity = !JobTriggerWasTraced
            ? null
            : LexBoxActivitySource.Get().StartActivity(ActivityKind.Internal,
                links:
                [
                    new ActivityLink(new ActivityContext(
                        ActivityTraceId.CreateFromString(JobTriggerTraceId),
                        ActivitySpanId.CreateFromString(JobTriggerSpanParentId),
                        ActivityTraceFlags.None
                    ))
                ]
            );
        try
        {
            await ExecuteJob(context);
        }
        catch (Exception e)
        {
            activity?.RecordException(e);
            throw new JobExecutionException(e);
        }
    }

    protected abstract Task ExecuteJob(IJobExecutionContext context);
}
