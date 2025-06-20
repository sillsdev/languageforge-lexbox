using System.Collections.Frozen;
using System.Net.Http.Json;
using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.AppUpdate;

public class UpdateChecker(
    IHttpClientFactory httpClientFactory,
    ILogger logger,
    GlobalEventBus eventBus,
    IPlatformUpdateService? platformUpdateServicesOptional = null): BackgroundService
{
    private const string FwliteUpdateUrlEnvVar = "FWLITE_UPDATE_URL";
    private const string ForceUpdateCheckEnvVar = "FWLITE_FORCE_UPDATE_CHECK";
    private const string PreventUpdateCheckEnvVar = "FWLITE_PREVENT_UPDATE";

    private static readonly FrozenSet<string> ValidPositiveEnvVarValues =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase, ["1", "true", "yes"]);
    private IPlatformUpdateService PlatformUpdateService =>
        platformUpdateServicesOptional ?? throw new InvalidOperationException("Platform update services not set");

    public static string GetUpdateUrl(UpdateRequest request)
    {
        return Environment.GetEnvironmentVariable(FwliteUpdateUrlEnvVar) ??
               $"https://lexbox.org/api/fwlite-release/should-update?appVersion={request.AppVersion}&edition={request.Edition}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (platformUpdateServicesOptional is null) return;
        await TryUpdate(PlatformUpdateService.UpdateRequest);
    }

    public async Task TryUpdate(UpdateRequest request, bool forceCheck = false)
    {
        if (!ShouldCheckForUpdate() && !forceCheck) return;
        var response = await ShouldUpdateAsync(request);

        PlatformUpdateService.LastUpdateCheck = (DateTime.UtcNow);
        if (!response.Update) return;
        if (ShouldPromptBeforeUpdate() &&
            !await PlatformUpdateService.RequestPermissionToUpdate(response.Release))
        {
            return;
        }

        UpdateResult updateResult = UpdateResult.ManualUpdateRequired;
        if (PlatformUpdateService.SupportsAutoUpdate)
        {
            updateResult = await PlatformUpdateService.ApplyUpdate(response.Release);
        }

        NotifyResult(updateResult);
    }

    private void NotifyResult(UpdateResult result)
    {
        eventBus.PublishEvent(new AppUpdateEvent(result));
    }

    private bool ShouldCheckForUpdate()
    {
        if (ValidPositiveEnvVarValues.Contains(Environment.GetEnvironmentVariable(PreventUpdateCheckEnvVar) ?? ""))
        {
            logger.LogInformation("Update check prevented by env var {EnvVar}", PreventUpdateCheckEnvVar);
            return false;
        }
        if (ValidPositiveEnvVarValues.Contains(Environment.GetEnvironmentVariable(ForceUpdateCheckEnvVar) ?? ""))
        {
            logger.LogInformation("Should check for update based on env var {EnvVar}", ForceUpdateCheckEnvVar);
            return true;
        }

        var lastChecked = PlatformUpdateService.LastUpdateCheck;
        var timeSinceLastCheck = DateTime.UtcNow - lastChecked;
        if (timeSinceLastCheck.TotalHours < -1)
        {
            logger.LogInformation("Should check for update, because last check was in the future: {LastCheck}",
                lastChecked);
            return true;
        }

        if (timeSinceLastCheck.TotalHours < 8)
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
        return PlatformUpdateService.IsOnMeteredConnection();
    }

    private async Task<ShouldUpdateResponse> ShouldUpdateAsync(UpdateRequest request)
    {
        try
        {
            var response = await httpClientFactory
                .CreateClient("Lexbox")
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, GetUpdateUrl(request))
                {
                    Headers = { { "User-Agent", $"Fieldworks-Lite-Client/{request.AppVersion}" } }
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
