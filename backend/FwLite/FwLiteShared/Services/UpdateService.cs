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
        return Task.Run(async () => await updateChecker.CheckForUpdate());
    }

    [JSInvokable]
    public Task<UpdateResult> ApplyUpdate(AvailableUpdate update)
    {
        return Task.Run(async () => await updateChecker.ApplyUpdate(update.Release));
    }
}
