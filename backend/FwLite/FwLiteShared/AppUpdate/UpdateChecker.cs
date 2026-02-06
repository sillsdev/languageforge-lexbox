using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.AppUpdate;

public class UpdateChecker(
    ILogger<UpdateChecker> logger,
    IOptions<FwLiteConfig> config,
    GlobalEventBus eventBus,
    IPlatformUpdateService platformUpdateService,
    IMemoryCache cache) : BackgroundService
{
    private const string CacheKey = "ManualUpdateCheck";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await TryUpdate();
    }

    public async Task<UpdateResult?> TryUpdate()
    {
        if (!ShouldCheckForUpdate()) return null;
        var update = await CheckForUpdate();
        if (update is null) return null;
        return await ApplyUpdate(update.Release);
    }

    public async Task<AvailableUpdate?> CheckForUpdate()
    {
        return await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var response = await platformUpdateService.ShouldUpdateAsync();
            platformUpdateService.LastUpdateCheck = DateTime.UtcNow;

            return response.Update
                ? new AvailableUpdate(response.Release, platformUpdateService.SupportsAutoUpdate)
                : null;
        });
    }

    public async Task<UpdateResult> ApplyUpdate(FwLiteRelease release)
    {
        if (ShouldPromptBeforeUpdate() &&
            !await platformUpdateService.RequestPermissionToUpdate(release))
        {
            return UpdateResult.Disallowed;
        }

        var updateResult = UpdateResult.ManualUpdateRequired;
        if (platformUpdateService.SupportsAutoUpdate)
        {
            updateResult = await platformUpdateService.ApplyUpdate(release);
        }

        NotifyResult(updateResult, release);
        return updateResult;
    }

    private void NotifyResult(UpdateResult result, FwLiteRelease release)
    {
        eventBus.PublishEvent(new AppUpdateEvent(result, release));
    }

    internal bool ShouldCheckForUpdate()
    {
        if (config.Value.UpdateCheckCondition == UpdateCheckCondition.Never)
        {
            logger.LogInformation("Update check prevented by configuration");
            return false;
        }
        if (config.Value.UpdateCheckCondition == UpdateCheckCondition.Always)
        {
            logger.LogInformation("Update check forced by configuration");
            return true;
        }

        var lastChecked = platformUpdateService.LastUpdateCheck;
        var timeSinceLastCheck = DateTime.UtcNow - lastChecked;
        if (timeSinceLastCheck.TotalHours < -1)
        {
            logger.LogInformation("Should check for update, because last check was in the future: {LastCheck}",
                lastChecked);
            return true;
        }

        if (timeSinceLastCheck < config.Value.UpdateCheckInterval)
        {
            logger.LogInformation("Should not check for update, because last check was too recent: {LastCheck}",
                lastChecked);
            return false;
        }

        logger.LogInformation("Should check for update based on last check time: {LastCheck}", lastChecked);
        return true;
    }

    private bool ShouldPromptBeforeUpdate()
    {
        return platformUpdateService.IsOnMeteredConnection();
    }
}
