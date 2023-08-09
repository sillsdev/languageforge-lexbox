using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;
using Yarp.Telemetry.Consumption;

namespace LexSyncReverseProxy;

public class ForwarderTelemetryConsumer : IForwarderTelemetryConsumer
{
    public void OnContentTransferred(DateTime timestamp,
        bool isRequest,
        long contentLength,
        long iops,
        TimeSpan readTime,
        TimeSpan writeTime,
        TimeSpan firstReadTime)
    {
        var activity = Activity.Current;
        if (activity is null) return;
        var eventTags = new ActivityTagsCollection
        {
            { "readTime", readTime },
            { "writeTime", writeTime },
            { "firstReadTime", firstReadTime }
        };
        if (isRequest)
        {
            activity.AddEvent(new ("Request finished", timestamp, eventTags));
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

    public void OnForwarderStage(DateTime timestamp, ForwarderStage stage)
    {
        Activity.Current?.AddEvent(new(stage.ToString(), timestamp));
    }

    public void OnForwarderFailed(DateTime timestamp, ForwarderError error)
    {
        Activity.Current?.SetStatus(ActivityStatusCode.Error, "Forwarder failed: " + error.ToString());
        Activity.Current?.AddEvent(new("Forwarder failed", timestamp));
    }
}
