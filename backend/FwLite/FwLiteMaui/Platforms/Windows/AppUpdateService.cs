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
            args.TryGetValue(ActionKey, out var action);
            args.TryGetValue(NotificationIdKey, out var notificationId);
            HandleNotificationAction(action, notificationId, args);
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

    // Shown once the update is staged and only a restart is needed. The Restart button is the only restart
    // affordance for the background/auto-update path (the in-app dialog has its own button).
    private void ShowUpdateReadyNotification(FwLiteRelease latestRelease)
    {
        new ToastContentBuilder()
            .AddArgument(NotificationIdKey, $"update-ready-{Guid.NewGuid()}")
            .AddText("FieldWorks Lite update ready")
            .AddText($"Version {latestRelease.Version} is ready. Restart to finish installing.")
            .AddButton(new ToastButton()
                .SetContent("Restart")
                .AddArgument(ActionKey, "restart"))
            .Show(toast => toast.Tag = "update");
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

    private void HandleNotificationAction(string? action, string? notificationId, ToastArguments args)
    {
        if (action == "restart")
        {
            _ = Task.Run(RestartToApplyUpdate);
            return;
        }

        string? result = null;
        if (args.TryGetValue(ResultRefKey, out var resultKey) && resultKey is not null)
            args.TryGetValue(resultKey, out result);

        if (notificationId is null || !NotificationCompletionSources.TryGetValue(notificationId, out var tcs))
        {
            if (action == "download")
            {
                var release = result is null ? null : JsonSerializer.Deserialize<FwLiteRelease>(result);
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

        var result = await DownloadAndStage(latestRelease, quitOnUpdate);

        // When the update is staged and only a restart remains, notify with a Restart button. Skip when
        // quitOnUpdate (that path force-shuts-down and restarts on its own).
        if (result is UpdateResult.Success && !quitOnUpdate)
            ShowUpdateReadyNotification(latestRelease);

        return result;
    }

    private async Task<UpdateResult> DownloadAndStage(FwLiteRelease latestRelease, bool quitOnUpdate)
    {
        // Preferred path: download through a loopback proxy so we can report real byte progress. Only if
        // the proxy infrastructure itself fails do we fall back to handing PackageManager the URL directly,
        // so we never regress update capability.
        using var cts = new CancellationTokenSource(DeployUpperBound);
        try
        {
            try
            {
                var progress = new DownloadProgressReporter(eventBus, latestRelease);
                await using var proxy = await UpdateDownloadProxy.StartAsync(latestRelease.Url, logger, progress.Report, cts.Token);
                return await Deploy(proxy.LocalUri, quitOnUpdate, cts.Token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Proxy update path failed; falling back to direct install");
                return await Deploy(new Uri(latestRelease.Url), quitOnUpdate, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // The timeout cancels the package operation and tears down the proxy, so nothing keeps
            // downloading — report failure (not Started) so the UI offers a retry rather than a Restart
            // button that would try to apply an incomplete package.
            logger.LogWarning("Update did not finish staging within {Timeout}; treating as failed", DeployUpperBound);
            return UpdateResult.Failed;
        }
    }

    // Once the package is in use, Windows stages the update and defers registration until the next launch.
    // The historical hang here came from attaching a Progress handler: that drops the WinRT->Task
    // completion in .NET desktop apps (dotnet/wpf#4097, CsWinRT#1720), so the await never returned and the
    // UI sat on "Downloading…" forever. Without a Progress handler the operation completes once the package
    // is staged (registration deferred, i.e. IsRegistered=false) — that's the primary "ready to restart"
    // signal. As a defensive fallback we also poll Windows for a pending deferred registration, so a repeat
    // of the hang still resolves.
    private async Task<UpdateResult> Deploy(Uri packageUri, bool quitOnUpdate, CancellationToken ct)
    {
        var packageFullName = Windows.ApplicationModel.Package.Current.Id.FullName;
        var packageManager = new PackageManager();
        // No .Progress handler on purpose (see above). Byte progress comes from the proxy; stage progress
        // is logged separately by PackageUpdateLogger (PackageCatalog.PackageUpdating).
        var deployTask = packageManager.AddPackageByUriAsync(packageUri,
            new AddPackageOptions()
            {
                DeferRegistrationWhenPackagesAreInUse = true,
                ForceUpdateFromAnyVersion = true,
                ForceAppShutdown = quitOnUpdate
            }).AsTask(ct);

        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var stagedTask = WaitForRegistrationPending(packageFullName, pollCts.Token);
        try
        {
            var completed = await Task.WhenAny(deployTask, stagedTask);
            if (completed == stagedTask)
            {
                await stagedTask; // observe / surface cancellation
                Observe(deployTask); // deferred registration keeps running in the background; don't cancel it
                logger.LogInformation("Update staged (registration pending); restart to apply");
                return UpdateResult.Success;
            }

            var result = await deployTask;
            if (!string.IsNullOrEmpty(result.ErrorText))
            {
                logger.LogError(result.ExtendedErrorCode, "Failed to install update: {ErrorText}", result.ErrorText);
                return UpdateResult.Failed;
            }

            logger.LogInformation("Update deployed (registered now: {IsRegistered}); restart to apply", result.IsRegistered);
            return UpdateResult.Success;
        }
        finally
        {
            await pollCts.CancelAsync(); // stop the poll loop if the deploy path returned first
        }
    }

    // Upper bound so a genuinely stuck deployment can't pin the operation forever; the happy path resolves
    // in seconds/minutes once staging completes, well under this.
    private static readonly TimeSpan DeployUpperBound = TimeSpan.FromMinutes(30);

    private bool _registrationPendingApiUnavailable;

    // Poll (every 10s) until Windows reports our family has a newer package staged with registration
    // deferred. Lets cancellation propagate so the caller's upper-bound timeout still surfaces as Started.
    private async Task WaitForRegistrationPending(string packageFullName, CancellationToken ct)
    {
        while (!IsRegistrationPending(packageFullName))
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
    }

    // "Is a newer version staged for our package with registration deferred (waiting for restart)?"
    // NB: despite the API parameter being named packageFamilyName, it requires the package FULL name —
    // passing a family name throws E_INVALIDARG (see the WindowsAppSDK IDL @warning on this method). Kept
    // defensive: it relies on an FrameworkUdk primitive with undocumented failure modes, so on any error we
    // disable it and lean on the deploy operation completing instead — never abort or report a false ready.
    private bool IsRegistrationPending(string packageFullName)
    {
        if (_registrationPendingApiUnavailable) return false;
        try
        {
            return Microsoft.Windows.Management.Deployment.PackageDeploymentManager.GetDefault()
                .IsPackageRegistrationPending(packageFullName);
        }
        catch (Exception e)
        {
            _registrationPendingApiUnavailable = true;
            logger.LogWarning(e, "IsPackageRegistrationPending unavailable; relying on deployment completion");
            return false;
        }
    }

    private static void Observe(Task task) =>
        _ = task.ContinueWith(static t => _ = t.Exception, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

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

    // Restart flags: allow Restart Manager to relaunch us only when we're terminated for an update, not
    // for a crash/hang/reboot. (RESTART_NO_CRASH | RESTART_NO_HANG | RESTART_NO_REBOOT.)
    private const int RestartOnlyForUpdate = 0x1 | 0x2 | 0x8;

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int RegisterApplicationRestart(string? commandLine, int flags);

    public async Task RestartToApplyUpdate()
    {
        logger.LogInformation("Applying staged update and restarting");

        // NOT AppInstance.Restart: that terminates this process and CreateProcess-relaunches the *old* exe
        // path directly, which is not a package activation, so the deferred registration never applies and
        // we'd come back on the old version. Instead we let the deployment engine register the staged
        // package with ForceTargetApplicationShutdown — it shuts down the package (WebView2 children
        // included), completes the registration, and Restart Manager relaunches us as the new version.
        // RegisterApplicationRestart must be called before the shutdown so we get relaunched.
        var hr = RegisterApplicationRestart(null, RestartOnlyForUpdate);
        if (hr != 0)
            logger.LogWarning("RegisterApplicationRestart failed (0x{Hr:X8}); the app may not relaunch automatically", hr);

        var packageManager = new PackageManager();
        // No .Progress handler (same dropped-completion issue as Deploy). On success the engine terminates
        // this process, so the await does not return; we only get here on failure.
        var result = await packageManager.RegisterPackageByFamilyNameAsync(
            Windows.ApplicationModel.Package.Current.Id.FamilyName,
            [],
            DeploymentOptions.ForceTargetApplicationShutdown,
            null,
            []).AsTask();

        logger.LogError(result.ExtendedErrorCode,
            "Failed to register staged update: {ErrorText}. It remains staged and will apply the next time the app is launched.",
            result.ErrorText);
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
