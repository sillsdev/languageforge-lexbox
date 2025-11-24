using FwLiteShared.AppUpdate;
using LexCore.Entities;

namespace FwLiteShared.Events;

public class AppUpdateEvent(UpdateResult result, FwLiteRelease release) : IFwEvent
{
    public UpdateResult Result { get; } = result;
    public FwLiteRelease Release { get; } = release;
    public FwEventType Type => FwEventType.AppUpdate;
    public bool IsGlobal => true;
}
