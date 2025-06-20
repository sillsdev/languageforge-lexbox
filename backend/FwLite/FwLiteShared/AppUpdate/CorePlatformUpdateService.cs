using LexCore.Entities;

namespace FwLiteShared.AppUpdate;

public class CorePlatformUpdateService: IPlatformUpdateService
{
    public DateTime LastUpdateCheck
    {
        get
        {
            return DateTime.MinValue;
        }
        set
        {
            //no-op, does not track last update time
        }
    }

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
}
