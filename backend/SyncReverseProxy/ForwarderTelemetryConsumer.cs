using System.Diagnostics;
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
}
