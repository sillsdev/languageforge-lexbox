using FwLiteShared.AppUpdate;

namespace FwLiteShared.Events;

public class AppUpdateEvent(UpdateResult result) : IFwEvent
{
    public UpdateResult Result { get; } = result;
    public FwEventType Type => FwEventType.AppUpdate;
    public bool IsGlobal => true;
}
