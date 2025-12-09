using LexCore.Entities;

namespace FwLiteShared.Events;

public class AppUpdateProgressEvent(uint percentage, FwLiteRelease release) : IFwEvent
{
    public uint Percentage { get; } = percentage;
    public FwLiteRelease Release { get; } = release;
    public FwEventType Type => FwEventType.AppUpdateProgress;
    public bool IsGlobal => true;
}
