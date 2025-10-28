using FwLiteShared.AppUpdate;
using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public class UpdateService(UpdateChecker updateChecker)
{
    [JSInvokable]
    public Task CheckForUpdates()
    {
        return updateChecker.TryUpdate(forceCheck: true);
    }
}
