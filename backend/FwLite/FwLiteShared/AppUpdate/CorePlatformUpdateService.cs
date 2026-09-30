using LexCore.Entities;

namespace FwLiteShared.AppUpdate;

public class CorePlatformUpdateService: IPlatformUpdateService
{
    public bool IsOnMeteredConnection()
    {
        return false;
    }

    public bool SupportsAutoUpdate => false;

    public Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease)
    {
        return Task.FromResult(UpdateResult.Unknown);
    }

    public Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease)
    {
        return Task.FromResult(true);
    }

    public Task RestartToApplyUpdate()
    {
        //no-op, restart-to-update is not supported on this platform
        return Task.CompletedTask;
    }
}
