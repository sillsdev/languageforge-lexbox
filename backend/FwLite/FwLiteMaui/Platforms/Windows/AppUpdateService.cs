using System.Diagnostics;
using System.Text.Json;
using Windows.Management.Deployment;
using Windows.Networking.Connectivity;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;

namespace FwLiteMaui;

public class AppUpdateService(ILogger<AppUpdateService> logger, IPreferences preferences, GlobalEventBus eventBus)
    : IMauiInitializeService, IPlatformUpdateService
{
    private const string LastUpdateCheckKey = "lastUpdateChecked";
    private  const string NotificationIdKey = "notificationId";
    private const string ActionKey = "action";
    private const string ResultRefKey = "resultRef";
    private static readonly Dictionary<string, TaskCompletionSource<string?>> NotificationCompletionSources = new();

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

    public async Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease)
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

    public async Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease)
    {
        return await ApplyUpdate(latestRelease, false);
    }
    private async Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease, bool quitOnUpdate)
    {
        logger.LogInformation("Installing new version: {Version}, Current version: {CurrentVersion}", latestRelease.Version, AppVersion.Version);
        ShowUpdateInstallingNotification(latestRelease);

        // Preferred path: download through a loopback proxy so we can report real download
        // progress. On any failure fall back to handing PackageManager the GitHub URL directly,
        // which is what we did before this change — we must never regress update capability.
        try
        {
            var progress = new DownloadProgressReporter(eventBus, latestRelease);
            await using var proxy = await UpdateDownloadProxy.StartAsync(latestRelease.Url, logger, progress.Report);
            return await Deploy(proxy.LocalUri, latestRelease, quitOnUpdate);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Proxy update path failed; falling back to direct install");
            return await Deploy(new Uri(latestRelease.Url), latestRelease, quitOnUpdate);
        }
    }

    private async Task<UpdateResult> Deploy(Uri packageUri, FwLiteRelease latestRelease, bool quitOnUpdate)
    {
        var packageManager = new PackageManager();
        var asyncOperation = packageManager.AddPackageByUriAsync(packageUri,
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
            logger.LogInformation("Deploying update: {ProgressPercentage}%", progressInfo.percentage);
        };

        //note this asyncOperation is not reliable, it's possible the update will install and this will never resolve
        var updateTask = asyncOperation.AsTask();
        var completedTask = await Task.WhenAny(updateTask, Task.Delay(TimeSpan.FromMinutes(2)));
        if (completedTask == updateTask)
        {
            var result = await updateTask;
            if (!string.IsNullOrEmpty(result.ErrorText))
            {
                logger.LogError(result.ExtendedErrorCode, "Failed to download update: {ErrorText}", result.ErrorText);
                return UpdateResult.Failed;
            }

            logger.LogInformation("Update downloaded, will install on next restart");
            return UpdateResult.Success;
        }

        return UpdateResult.Started;
    }

    // Turns the proxy's running byte total into throttled progress events (bytes + speed).
    // The JsEventListener channel is small (size 10) so we cap emission at ~4/sec.
    private sealed class DownloadProgressReporter(GlobalEventBus eventBus, FwLiteRelease release)
    {
        private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(250);
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();
        private readonly Lock _lock = new();
        private long _lastEmitTimestamp;

        public void Report(long totalBytesDownloaded)
        {
            var now = Stopwatch.GetTimestamp();
            lock (_lock)
            {
                if (_lastEmitTimestamp != 0 && Stopwatch.GetElapsedTime(_lastEmitTimestamp, now) < MinInterval)
                    return;
                _lastEmitTimestamp = now;
            }

            var elapsedSeconds = Stopwatch.GetElapsedTime(_startTimestamp, now).TotalSeconds;
            var bytesPerSecond = elapsedSeconds > 0 ? totalBytesDownloaded / elapsedSeconds : 0;
            eventBus.PublishEvent(new AppUpdateProgressEvent(totalBytesDownloaded, bytesPerSecond, release));
        }
    }

    public DateTime LastUpdateCheck
    {
        get => preferences.Get(LastUpdateCheckKey, DateTime.MinValue);
        set => preferences.Set(LastUpdateCheckKey, value);
    }

    public bool SupportsAutoUpdate => !FwLiteMauiKernel.IsPortableApp;

    public bool IsOnMeteredConnection()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile == null) return false;
        var cost = profile.GetConnectionCost();
        return cost.NetworkCostType != NetworkCostType.Unrestricted;
    }
}
