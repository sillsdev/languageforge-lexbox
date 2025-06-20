using LexCore.Entities;
using Microsoft.Extensions.Options;

namespace FwLiteShared.AppUpdate;

public class CorePlatformUpdateService(IOptions<FwLiteConfig> options): IPlatformUpdateService
{
    public UpdateRequest UpdateRequest => new(options.Value.AppVersion, options.Value.Os switch
        {
            FwLitePlatform.Android => FwLiteEdition.Android,
            FwLitePlatform.iOS => FwLiteEdition.iOS,
            FwLitePlatform.Linux => FwLiteEdition.Linux,
            FwLitePlatform.Mac => FwLiteEdition.Mac,
            FwLitePlatform.Windows => FwLiteEdition.Windows,
            _ => throw new NotSupportedException($"Unsupported platform: {options.Value.Os}")
        });

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
