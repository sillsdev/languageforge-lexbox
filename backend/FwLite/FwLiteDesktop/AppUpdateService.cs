using System.Buffers;
using System.Net.Http.Json;
using Windows.Management.Deployment;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace FwLiteDesktop;

public class AppUpdateService(
    IHttpClientFactory httpClientFactory,
    ILogger<AppUpdateService> logger,
    IPreferences preferences) : IMauiInitializeService
{


    private const string LastUpdateCheck = "lastUpdateChecked";
    private const string FwliteUpdateUrlEnvVar = "FWLITE_UPDATE_URL";
    private const string ForceUpdateCheckEnvVar = "FWLITE_FORCE_UPDATE_CHECK";
    private static readonly SearchValues<string> ValidPositiveEnvVarValues = SearchValues.Create([ "1", "true", "yes" ], StringComparison.OrdinalIgnoreCase);
    private static readonly string UpdateUrl = Environment.GetEnvironmentVariable(FwliteUpdateUrlEnvVar) ?? "https://lexbox.org/api/fwlite-release/latest";

    public void Initialize(IServiceProvider services)
    {
        _ = Task.Run(TryUpdate);
    }

    private async Task TryUpdate()
    {
        if (!ShouldCheckForUpdate()) return;
        var latestRelease = await FetchRelease();
        if (latestRelease is null) return;
        var currentVersion = AppVersion.Version;
        var shouldUpdateToRelease = String.Compare(latestRelease.Version, currentVersion, StringComparison.Ordinal) > 0;
        if (!shouldUpdateToRelease)
        {
            logger.LogInformation("Version {CurrentVersion} is more recent than latest release {LatestRelease}, not updating", currentVersion, latestRelease.Version);
            return;
        }

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
            var latestRelease = await httpClientFactory
                .CreateClient("Lexbox")
                .GetFromJsonAsync<FwLiteRelease>(UpdateUrl);
            return latestRelease;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to fetch latest release");
            return null;
        }
    }

    private bool ShouldCheckForUpdate()
    {
        if (ValidPositiveEnvVarValues.Contains(Environment.GetEnvironmentVariable(ForceUpdateCheckEnvVar) ?? ""))
            return true;
        var lastChecked = preferences.Get(LastUpdateCheck, DateTime.MinValue);
        if (lastChecked.AddDays(1) > DateTime.UtcNow) return false;
        preferences.Set(LastUpdateCheck, DateTime.UtcNow);
        return true;
    }
}
