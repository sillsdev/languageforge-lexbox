using System.Net.Http.Json;
using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.AppUpdate;

public record AvailableUpdate(FwLiteRelease Release, bool SupportsAutoUpdate);

public class UpdateChecker(
    IHttpClientFactory httpClientFactory,
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
            var response = await ShouldUpdateAsync();
            platformUpdateService.LastUpdateCheck = DateTime.UtcNow;
            return response.Update
                ? new AvailableUpdate(response.Release, platformUpdateService.SupportsAutoUpdate)
                : null;
        });
    }

    /// <summary>
    /// Finish installing an update the platform already downloaded (Android Play flexible flow). Invoked
    /// from the web UI's "Restart" prompt via <see cref="Services.UpdateService"/>.
    /// </summary>
    public Task CompleteUpdate() => platformUpdateService.CompleteUpdate();

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

        //Store-distributed platforms have no GitHub release feed we act on, so the round-trip is
        //pointless on every launch: iOS/Mac have no feed at all, and Android is driven by Google Play's
        //in-app updates (AndroidInAppUpdateService) instead of this check.
        //Left after the explicit config overrides so Always can still force a check for testing.
        if (config.Value.Os is FwLitePlatform.iOS or FwLitePlatform.Mac or FwLitePlatform.Android)
        {
            logger.LogInformation("Update check skipped: {Os} updates are store/Play-driven, not via the release feed",
                config.Value.Os);
            return false;
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

    private async Task<ShouldUpdateResponse> ShouldUpdateAsync()
    {
        try
        {
            var response = await httpClientFactory
                .CreateClient("Lexbox")
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, config.Value.UpdateUrl)
                {
                    Headers = { { "User-Agent", $"Fieldworks-Lite-Client/{config.Value.AppVersion}" } }
                });
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to get should update response: {StatusCode} {ResponseContent}",
                    response.StatusCode,
                    responseContent);
                return new ShouldUpdateResponse(null);
            }

            var result = await response.Content.ReadFromJsonAsync<ShouldUpdateResponse>();
            return result ?? new ShouldUpdateResponse(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch latest release");
            return new ShouldUpdateResponse(null);
        }
    }
}
