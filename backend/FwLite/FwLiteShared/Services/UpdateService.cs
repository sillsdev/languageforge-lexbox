using FwLiteShared.AppUpdate;
using Microsoft.JSInterop;
using Reinforced.Typings.Attributes;

namespace FwLiteShared.Services;

public class UpdateService(UpdateChecker updateChecker)
{
    [JSInvokable]
    [TsFunction(Type = "Promise<IAvailableUpdate | null>")]
    public Task<AvailableUpdate?> CheckForUpdates()
    {
        return updateChecker.CheckForUpdate();
    }

    [JSInvokable]
    public Task<UpdateResult> ApplyUpdate(AvailableUpdate update)
    {
        return updateChecker.ApplyUpdate(update.Release);
    }
}
