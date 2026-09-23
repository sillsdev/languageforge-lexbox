using FwLiteShared.AppUpdate;
using Microsoft.JSInterop;
using Reinforced.Typings.Attributes;

namespace FwLiteShared.Services;

public class UpdateService(UpdateChecker updateChecker)
{
    [JSInvokable]
    public Task<AvailableUpdate?> CheckForUpdates()
    {
        return Task.Run(async () => await updateChecker.CheckForUpdate());
    }

    [JSInvokable]
    public Task<UpdateResult> ApplyUpdate(AvailableUpdate update)
    {
        return Task.Run(async () => await updateChecker.ApplyUpdate(update.Release));
    }

    /// <summary>
    /// Finish installing an update the platform already downloaded (Android Play flexible flow), invoked
    /// from the "Restart" toast. A no-op on platforms that don't need it.
    /// </summary>
    [JSInvokable]
    public Task CompleteUpdate()
    {
        return updateChecker.CompleteUpdate();
    }
}
