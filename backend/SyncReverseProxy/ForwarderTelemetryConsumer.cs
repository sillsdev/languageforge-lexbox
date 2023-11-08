using System.Diagnostics;
using System.Runtime.CompilerServices;
using Yarp.ReverseProxy.Forwarder;
using Yarp.Telemetry.Consumption;

namespace LexSyncReverseProxy;

public class ForwarderTelemetryConsumer : IForwarderTelemetryConsumer
{
    private readonly ILogger<ForwarderTelemetryConsumer> _logger;

    public ForwarderTelemetryConsumer(ILogger<ForwarderTelemetryConsumer> logger)
    {
        _logger = logger;
    }

    public void OnContentTransferred(DateTime timestamp,
        bool isRequest,
        long contentLength,
        long iops,
        TimeSpan readTime,
        TimeSpan writeTime,
        TimeSpan firstReadTime)
    {
        _logger.LogInformation("Content transferred, {Type} content length {ContentLength} bytes",
            isRequest ? "Request" : "Response",
            contentLength.ToString("N0"));
        var activity = Activity.Current;
        if (activity is null) return;
        var eventTags = new ActivityTagsCollection
        {
            { "readTime", readTime }, { "writeTime", writeTime }, { "firstReadTime", firstReadTime }
        };
        if (isRequest)
        {
            activity.AddEvent(new("Request finished", timestamp, eventTags));
            activity.SetTag("http.request.body.size", contentLength);
            activity.SetTag("http.request.iops", iops);
        }
        else
        {
            activity.AddEvent(new("Response finished", timestamp, eventTags));
            activity.SetTag("http.response.body.size", contentLength);
            activity.SetTag("http.response.iops", iops);
        }
    }

    private record ContentTransferringData(int EventCount)
    {
        public int EventCount { get; set; } = EventCount;
    }

    private ConditionalWeakTable<Activity, ContentTransferringData> _activityData = new();

    public void OnContentTransferring(DateTime timestamp,
        bool isRequest,
        long contentLength,
        long iops,
        TimeSpan readTime,
        TimeSpan writeTime)
    {
        _logger.LogInformation("Content transferring, {ContentLength} bytes", contentLength.ToString("N0"));
        var activity = Activity.Current;
        if (activity is null) return;
        //we use this activity data weak weak map to keep track of how many events we've added.
        //it's a very expensive calculation becase Count() does a walk through a linked list.
        var data = _activityData.GetValue(activity, static a => new ContentTransferringData(a.Events.Count()));
        // the max number of events seems to be 128 (we may be able to override that default, but this seems fine)
        // we want to stop before we get there as other events are more important
        if (data.EventCount > 110) //todo, it would be nice to slow down the rate at which we record this event instead of just abruptly stopping at 110
        {
            return;
        }

        activity.AddEvent(new("Content transferring",
            timestamp,
            new()
            {
                { "contentLength", contentLength },
                { "iops", iops },
                { "readTime", readTime },
                { "writeTime", writeTime }
            }));
        data.EventCount++;
    }

    public void OnForwarderStage(DateTime timestamp, ForwarderStage stage)
    {
        Activity.Current?.AddEvent(new(stage.ToString(), timestamp));
    }

    public void OnForwarderFailed(DateTime timestamp, ForwarderError error)
    {
        var traceId = Activity.Current?.TraceId;
        _logger.LogError("Forwarder Failed: {Error}. Trace ID: {TraceID}.", error.ToString(), traceId);
        Activity.Current?.SetStatus(ActivityStatusCode.Error, "Forwarder failed: " + error.ToString());
        Activity.Current?.AddEvent(new("Forwarder failed", timestamp));
    }
}
