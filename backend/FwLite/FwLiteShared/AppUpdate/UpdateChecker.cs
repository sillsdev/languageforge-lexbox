using System.Net.Http.Json;
using FwLiteShared.Events;
using LexCore.Entities;
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
    IPlatformUpdateService platformUpdateService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await TryUpdate();
    }

    public async Task TryUpdate()
    {
        if (!ShouldCheckForUpdate()) return;
        var update = await CheckForUpdate();
        if (update is null) return;
        await ApplyUpdate(update.Release);
    }

    public async Task<AvailableUpdate?> CheckForUpdate()
    {
        // todo maybe there should be a memory cache here for at least a couple minutes?
        var response = await ShouldUpdateAsync();
        platformUpdateService.LastUpdateCheck = DateTime.UtcNow;
        if (!response.Update) return null;
        return new AvailableUpdate(response.Release, platformUpdateService.SupportsAutoUpdate);
    }

    public async Task ApplyUpdate(FwLiteRelease release)
    {
        if (ShouldPromptBeforeUpdate() &&
            !await platformUpdateService.RequestPermissionToUpdate(release))
        {
            return;
        }

        UpdateResult updateResult = UpdateResult.ManualUpdateRequired;
        if (platformUpdateService.SupportsAutoUpdate)
        {
            updateResult = await platformUpdateService.ApplyUpdate(release);
        }

        NotifyResult(updateResult);
    }

    private void NotifyResult(UpdateResult result)
    {
        eventBus.PublishEvent(new AppUpdateEvent(result));
    }

    private bool ShouldCheckForUpdate()
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
