using System.Diagnostics;
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
            contentLength.ToString("N"));
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

    public void OnContentTransferring(DateTime timestamp,
        bool isRequest,
        long contentLength,
        long iops,
        TimeSpan readTime,
        TimeSpan writeTime)
    {
        _logger.LogInformation("Content transferring, {ContentLength} bytes", contentLength.ToString("N"));
        Activity.Current?.AddEvent(new("Content transferring",
            timestamp,
            new()
            {
                { "contentLength", contentLength },
                { "iops", iops },
                { "readTime", readTime },
                { "writeTime", writeTime }
            }));
    }

    public void OnForwarderStage(DateTime timestamp, ForwarderStage stage)
    {
        Activity.Current?.AddEvent(new(stage.ToString(), timestamp));
    }

    public void OnForwarderFailed(DateTime timestamp, ForwarderError error)
    {
        _logger.LogError("Forwarder Failed: {Error}", error.ToString());
        Activity.Current?.SetStatus(ActivityStatusCode.Error, "Forwarder failed: " + error.ToString());
        Activity.Current?.AddEvent(new("Forwarder failed", timestamp));
    }
}
