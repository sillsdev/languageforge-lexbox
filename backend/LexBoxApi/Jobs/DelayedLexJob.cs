using System.Diagnostics;
using LexBoxApi.Otel;
using OpenTelemetry.Trace;
using Quartz;

namespace LexBoxApi.Jobs;

public abstract class DelayedLexJob() : LexJob
{
    protected static async Task QueueJob(ISchedulerFactory schedulerFactory,
        JobKey key,
        JobDataMap data,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        data[nameof(JobTriggerTraceId)] = Activity.Current?.Context.TraceId.ToHexString() ?? string.Empty;
        data[nameof(JobTriggerSpanParentId)] = Activity.Current?.Context.SpanId.ToHexString() ?? string.Empty;
        var trigger = TriggerBuilder.Create()
            // TODO: Is there a simpler way of telling Quartz "Hey, enqueue this job after X delay"? Picking a unique trigger name each time seems unnecessarily complicated.
            .WithIdentity(key.Name + "_Trigger_" + now.Ticks.ToString(), key.Group)
            .StartAt(now.Add(delay))
            .ForJob(key.Name, key.Group)
            .UsingJobData(data)
            .Build();
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }
}
