using System.Buffers;
using System.Net.Http.Json;
using Windows.Management.Deployment;
using LexCore.Entities;
using Microsoft.Extensions.Logging;

namespace FwLiteDesktop;

public class AppUpdateService(
    IHttpClientFactory httpClientFactory,
    ILogger<AppUpdateService> logger,
    IPreferences preferences,
    IConnectivity connectivity) : IMauiInitializeService
{
    private const string LastUpdateCheck = "lastUpdateChecked";
    private const string FwliteUpdateUrlEnvVar = "FWLITE_UPDATE_URL";
    private const string ForceUpdateCheckEnvVar = "FWLITE_FORCE_UPDATE_CHECK";
    private const string PreventUpdateCheckEnvVar = "FWLITE_PREVENT_UPDATE";

    private static readonly SearchValues<string> ValidPositiveEnvVarValues =
        SearchValues.Create(["1", "true", "yes"], StringComparison.OrdinalIgnoreCase);

    private static readonly string UpdateUrl = Environment.GetEnvironmentVariable(FwliteUpdateUrlEnvVar) ??
                                               $"https://lexbox.org/api/fwlite-release/latest?appVersion={AppVersion.Version}";

    public void Initialize(IServiceProvider services)
    {
        _ = Task.Run(TryUpdate);
    }

    private async Task TryUpdate()
    {
        if (ValidPositiveEnvVarValues.Contains(Environment.GetEnvironmentVariable(PreventUpdateCheckEnvVar) ?? ""))
        {
            logger.LogInformation("Update check prevented by env var {EnvVar}", PreventUpdateCheckEnvVar);
            return;
        }

        if (!ShouldCheckForUpdate()) return;
        var latestRelease = await FetchRelease();
        if (latestRelease is null) return;
        var currentVersion = AppVersion.Version;
        var shouldUpdateToRelease = String.Compare(latestRelease.Version, currentVersion, StringComparison.Ordinal) > 0;
        if (!shouldUpdateToRelease)
        {
            logger.LogInformation(
                "Version {CurrentVersion} is more recent than latest release {LatestRelease}, not updating",
                currentVersion,
                latestRelease.Version);
            return;
        }

        await ApplyUpdate(latestRelease);
    }

    private async Task ApplyUpdate(FwLiteRelease latestRelease)
    {
        logger.LogInformation("New version available: {Version}", latestRelease.Version);
        var packageManager = new PackageManager();
        var asyncOperation = packageManager.AddPackageAsync(new Uri(latestRelease.Url), [], DeploymentOptions.None);
        asyncOperation.Progress = (info, progressInfo) =>
        {
            logger.LogInformation("Downloading update: {ProgressPercentage}%", progressInfo.percentage);
        };
        var result = await asyncOperation.AsTask();
        if (!string.IsNullOrEmpty(result.ErrorText))
        {
            logger.LogError(result.ExtendedErrorCode, "Failed to download update: {ErrorText}", result.ErrorText);
            return;
        }

        logger.LogInformation("Update downloaded, will install on next restart");
    }

    private async Task<FwLiteRelease?> FetchRelease()
    {
        try
        {
            var response = await httpClientFactory
                .CreateClient("Lexbox")
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, UpdateUrl)
                {
                    Headers = { { "User-Agent", $"Fieldworks-Lite-Client/{AppVersion.Version}" } }
                });
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to get latest release from github: {StatusCode} {ResponseContent}",
                    response.StatusCode,
                    responseContent);
                return null;
            }

            var latestRelease = await response.Content.ReadFromJsonAsync<FwLiteRelease>();
            return latestRelease;
        }
        catch (Exception e)
        {
            if (connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                logger.LogError(e, "Failed to fetch latest release");
            }
            else
            {
                logger.LogInformation(e, "Failed to fetch latest release, no internet connection");
            }

            return null;
        }
    }

    private bool ShouldCheckForUpdate()
    {
        if (ValidPositiveEnvVarValues.Contains(Environment.GetEnvironmentVariable(ForceUpdateCheckEnvVar) ?? ""))
            return true;
        var lastChecked = preferences.Get(LastUpdateCheck, DateTime.MinValue);
        var timeSinceLastCheck = DateTime.UtcNow - lastChecked;
        //if last checked is in the future (should never happen), then we want to reset the time and check again
        if (timeSinceLastCheck.Hours < -1)
        {
            preferences.Set(LastUpdateCheck, DateTime.UtcNow);
            return true;
        }
        if (timeSinceLastCheck.Hours < 20) return false;
        preferences.Set(LastUpdateCheck, DateTime.UtcNow);
        return true;
    }
}
