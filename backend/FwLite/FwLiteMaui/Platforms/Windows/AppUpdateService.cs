using System.Buffers;
using System.Net.Http.Json;
using System.Text.Json;
using Windows.Management.Deployment;
using Windows.Networking.Connectivity;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace FwLiteMaui;

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
    private  const string NotificationIdKey = "notificationId";
    private const string ActionKey = "action";
    private const string ResultRefKey = "resultRef";
    private static readonly Dictionary<string, TaskCompletionSource<string?>> NotificationCompletionSources = new();

    private static readonly SearchValues<string> ValidPositiveEnvVarValues =
        SearchValues.Create(["1", "true", "yes"], StringComparison.OrdinalIgnoreCase);

    private static readonly string ShouldUpdateUrl = Environment.GetEnvironmentVariable(FwliteUpdateUrlEnvVar) ??
                                               $"https://lexbox.org/api/fwlite-release/should-update?appVersion={AppVersion.Version}";

    public void Initialize(IServiceProvider services)
    {
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
            HandleNotificationAction(args.Get(ActionKey), args.Get(NotificationIdKey), args);
        };
        if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            //don't check for updates if the user already clicked on a notification
            return;
        }
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
        var response = await ShouldUpdate();
        if (!response.Update) return;
        if (ShouldPromptBeforeUpdate() && !await RequestPermissionToUpdate(response.Release))
        {
            return;
        }
        await ApplyUpdate(response.Release);
    }

    private async Task Test()
    {
        logger.LogInformation("Testing update notifications");
        var fwLiteRelease = new FwLiteRelease("1.0.0.0", "https://test.com");
        if (!await RequestPermissionToUpdate(fwLiteRelease))
        {
            logger.LogInformation("User declined update");
            return;
        }

        await ApplyUpdate(fwLiteRelease);
    }

    private void ShowUpdateInstallingNotification(FwLiteRelease latestRelease)
    {
        new ToastContentBuilder().AddText("FieldWorks Lite Installing update").AddText($"Version {latestRelease.Version} will be installed after FieldWorks Lite is closed").Show();
    }

    private async Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease)
    {
        var notificationId = $"update-{Guid.NewGuid()}";
        var tcs = new TaskCompletionSource<string?>();
        NotificationCompletionSources.Add(notificationId, tcs);
        new ToastContentBuilder()
            .AddText("FieldWorks Lite Update")
            .AddText("A new version of FieldWorks Lite is available")
            .AddText($"Version {latestRelease.Version} would you like to download and install this update?")
            .AddArgument(NotificationIdKey, notificationId)
            .AddButton(new ToastButton()
                .SetContent("Download & Install")
                .AddArgument(ActionKey, "download")
                .AddArgument("release", JsonSerializer.Serialize(latestRelease)))
                .AddArgument(ResultRefKey, "release")
            .Show(toast =>
            {
                toast.Tag = "update";
            });
        var taskResult = await tcs.Task;
        return taskResult != null;
    }

    private void HandleNotificationAction(string action, string notificationId, ToastArguments args)
    {
        var result = args.Get(args.Get(ResultRefKey));
        if (!NotificationCompletionSources.TryGetValue(notificationId, out var tcs))
        {
            if (action == "download")
            {
                var release = JsonSerializer.Deserialize<FwLiteRelease>(result);
                if (release == null)
                {
                    logger.LogError("Invalid release {Release} for notification {NotificationId}", result, notificationId);
                    return;
                }
                _ = Task.Run(() => ApplyUpdate(release, true));
            }
            else
            {
                logger.LogError("Unknown action {Action} for notification {NotificationId}", action, notificationId);
            }
            return;
        }

        tcs.SetResult(result);
        NotificationCompletionSources.Remove(notificationId);
    }

    private async Task ApplyUpdate(FwLiteRelease latestRelease, bool quitOnUpdate = false)
    {
        logger.LogInformation("Installing new version: {Version}, Current version: {CurrentVersion}", latestRelease.Version, AppVersion.Version);
        var packageManager = new PackageManager();
        var asyncOperation = packageManager.AddPackageByUriAsync(new Uri(latestRelease.Url),
            new AddPackageOptions()
            {
                DeferRegistrationWhenPackagesAreInUse = true,
                ForceUpdateFromAnyVersion = true,
                ForceAppShutdown = quitOnUpdate
            });
        asyncOperation.Progress = (info, progressInfo) =>
        {
            if (progressInfo.state == DeploymentProgressState.Queued)
            {
                logger.LogInformation("Queued update");
                return;
            }
            logger.LogInformation("Downloading update: {ProgressPercentage}%", progressInfo.percentage);
        };
        ShowUpdateInstallingNotification(latestRelease);

        //note this asyncOperation is not reliable, it's possible the update will install and this will never resolve, so don't do anything important after this
        var result = await asyncOperation;
        if (!string.IsNullOrEmpty(result.ErrorText))
        {
            logger.LogError(result.ExtendedErrorCode, "Failed to download update: {ErrorText}", result.ErrorText);
            return;
        }

        logger.LogInformation("Update downloaded, will install on next restart");
    }

    private async Task<ShouldUpdateResponse> ShouldUpdate()
    {
        try
        {
            var response = await httpClientFactory
                .CreateClient("Lexbox")
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, ShouldUpdateUrl)
                {
                    Headers = { { "User-Agent", $"Fieldworks-Lite-Client/{AppVersion.Version}" } }
                });
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to get should update response lexbox: {StatusCode} {ResponseContent}",
                    response.StatusCode,
                    responseContent);
                return new ShouldUpdateResponse(null);
            }

            return await response.Content.ReadFromJsonAsync<ShouldUpdateResponse>() ?? new ShouldUpdateResponse(null);
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

            return new ShouldUpdateResponse(null);
        }
    }

    private bool ShouldCheckForUpdate()
    {
        if (ValidPositiveEnvVarValues.Contains(Environment.GetEnvironmentVariable(ForceUpdateCheckEnvVar) ?? ""))
        {
            logger.LogInformation("Should check for update based on env var {EnvVar}", ForceUpdateCheckEnvVar);
            return true;
        }
        var lastChecked = preferences.Get(LastUpdateCheck, DateTime.MinValue);
        var timeSinceLastCheck = DateTime.UtcNow - lastChecked;
        //if last checked is in the future (should never happen), then we want to reset the time and check again
        if (timeSinceLastCheck.TotalHours < -1)
        {
            preferences.Set(LastUpdateCheck, DateTime.UtcNow);
            logger.LogInformation("Should check for update, because last check was in the future: {LastCheck}", lastChecked);
            return true;
        }
        if (timeSinceLastCheck.TotalHours < 8)
        {
            logger.LogInformation("Should not check for update, because last check was too recent: {LastCheck}", lastChecked);
            return false;
        }
        preferences.Set(LastUpdateCheck, DateTime.UtcNow);
        logger.LogInformation("Should check for update based on last check time: {LastCheck}", lastChecked);
        return true;
    }

    private bool ShouldPromptBeforeUpdate()
    {
        return IsOnMeteredConnection();
    }

    private bool IsOnMeteredConnection()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile == null) return false;
        var cost = profile.GetConnectionCost();
        return cost.NetworkCostType != NetworkCostType.Unrestricted;

    }
}
