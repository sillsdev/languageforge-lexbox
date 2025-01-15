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
            .WithIdentity(key.Name + "_Trigger", key.Group)
            .StartAt(now.Add(delay))
            .ForJob(key.Name, key.Group)
            .UsingJobData(data)
            .Build();
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }
}
