using LexCore.Entities;

namespace FwLiteShared.Events;

public class AppUpdateProgressEvent(long bytesDownloaded, double bytesPerSecond, FwLiteRelease release) : IFwEvent
{
    public long BytesDownloaded { get; } = bytesDownloaded;
    public double BytesPerSecond { get; } = bytesPerSecond;
    public FwLiteRelease Release { get; } = release;
    public FwEventType Type => FwEventType.AppUpdateProgress;
    public bool IsGlobal => true;
}
