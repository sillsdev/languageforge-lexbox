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
    public async Task ApplyUpdate(AvailableUpdate update)
    {
        await updateChecker.ApplyUpdate(update.Release);
    }
}
