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
    UpdateCheckThrottle throttle,
    IMemoryCache cache) : BackgroundService
{
    public const string HttpClientName = "Lexbox";
    private const string CacheKey = "ManualUpdateCheck";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await TryUpdate();
    }

    public async Task<UpdateResult?> TryUpdate()
    {
        if (!ShouldCheckReleaseFeed()) return null;
        var update = await CheckForUpdate();
        if (update is null) return null;
        return await ApplyUpdate(update.Release);
    }

    public async Task<AvailableUpdate?> CheckForUpdate()
    {
        if (cache.TryGetValue(CacheKey, out AvailableUpdate? cached)) return cached;
        var response = await ShouldUpdateAsync();
        //a request that never reached the server (offline, DNS still down while a VPN connects) is not a check:
        //leave the throttle and the manual-check cache alone so the next launch or connectivity recovery
        //retries instead of waiting out UpdateCheckInterval
        if (response is null) return null;
        throttle.RecordCheck();
        var update = response.Update
            ? new AvailableUpdate(response.Release, platformUpdateService.SupportsAutoUpdate)
            : null;
        cache.Set(CacheKey, update, CacheDuration);
        return update;
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

    /// <summary>
    /// Whether to check the GitHub release feed now: the shared interval gate
    /// (<see cref="UpdateCheckThrottle.ShouldCheckForUpdate"/>) plus a skip for store-distributed
    /// platforms. iOS/Mac have no release feed at all, and Android is driven by Google Play's in-app
    /// updates (AndroidInAppUpdateService) instead of this check. The explicit <c>Always</c> condition
    /// still forces a feed check for on-device endpoint testing.
    /// </summary>
    internal bool ShouldCheckReleaseFeed()
    {
        if (config.Value.UpdateCheckCondition != UpdateCheckCondition.Always &&
            config.Value.Os is FwLitePlatform.iOS or FwLitePlatform.Mac or FwLitePlatform.Android)
        {
            logger.LogInformation("Update check skipped: {Os} updates are store/Play-driven, not via the release feed",
                config.Value.Os);
            return false;
        }

        return throttle.ShouldCheckForUpdate();
    }

    private bool ShouldPromptBeforeUpdate()
    {
        return platformUpdateService.IsOnMeteredConnection();
    }

    /// <returns>The server's answer, or null when the request failed before getting a response.</returns>
    private async Task<ShouldUpdateResponse?> ShouldUpdateAsync()
    {
        try
        {
            var response = await httpClientFactory
                .CreateClient(HttpClientName)
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
            return null;
        }
    }
}
