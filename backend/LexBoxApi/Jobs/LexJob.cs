using System.Diagnostics;
using LexBoxApi.Otel;
using OpenTelemetry.Trace;
using Quartz;

namespace LexBoxApi.Jobs;

public abstract class LexJob : IJob
{
    public string? TraceParentId { get; set; }
    public string? SpanParentId { get; set; }
    private bool IsTracing => !string.IsNullOrEmpty(TraceParentId) && !string.IsNullOrEmpty(SpanParentId);

    protected static async Task QueueJob(ISchedulerFactory schedulerFactory,
        JobKey key,
        JobDataMap data,
        CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        data[nameof(TraceParentId)] = Activity.Current?.Context.TraceId.ToHexString() ?? string.Empty;
        data[nameof(SpanParentId)] = Activity.Current?.Context.SpanId.ToHexString() ?? string.Empty;
        await scheduler.TriggerJob(key, data, cancellationToken);
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        using var activity = !IsTracing
            ? null
            : LexBoxActivitySource.Get().StartActivity(ActivityKind.Internal,
                links:
                [
                    new ActivityLink(new ActivityContext(
                        ActivityTraceId.CreateFromString(TraceParentId),
                        ActivitySpanId.CreateFromString(SpanParentId),
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
