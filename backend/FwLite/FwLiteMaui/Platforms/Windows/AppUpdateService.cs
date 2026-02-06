using System.Text.Json;
using Windows.Management.Deployment;
using Windows.Networking.Connectivity;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using FwLiteShared;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;

namespace FwLiteMaui;

public class AppUpdateService : MauiPlatformUpdateServiceBase, IMauiInitializeService
{
    private readonly ILogger<AppUpdateService> _logger;
    private readonly GlobalEventBus _eventBus;

    private const string NotificationIdKey = "notificationId";
    private const string ActionKey = "action";
    private const string ResultRefKey = "resultRef";
    private static readonly Dictionary<string, TaskCompletionSource<string?>> NotificationCompletionSources = new();

    public AppUpdateService(
        IHttpClientFactory httpClientFactory,
        IOptions<FwLiteConfig> config,
        ILogger<AppUpdateService> logger,
        IPreferences preferences,
        GlobalEventBus eventBus)
        : base(httpClientFactory, config, logger, preferences)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

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
        _logger.LogInformation("Testing update notifications");
        var fwLiteRelease = new FwLiteRelease("1.0.0.0", "https://test.com");
        if (!await RequestPermissionToUpdate(fwLiteRelease))
        {
            _logger.LogInformation("User declined update");
            return;
        }

        await ApplyUpdate(fwLiteRelease);
    }

    private void ShowUpdateInstallingNotification(FwLiteRelease latestRelease)
    {
        new ToastContentBuilder().AddText("FieldWorks Lite Installing update").AddText($"Version {latestRelease.Version} will be installed after FieldWorks Lite is closed").Show();
    }

    public override async Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease)
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
                    _logger.LogError("Invalid release {Release} for notification {NotificationId}", result, notificationId);
                    return;
                }
                _ = Task.Run(() => ApplyUpdateInternal(release, true));
            }
            else
            {
                _logger.LogError("Unknown action {Action} for notification {NotificationId}", action, notificationId);
            }
            return;
        }

        tcs.SetResult(result);
        NotificationCompletionSources.Remove(notificationId);
    }

    public override async Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease)
    {
        return await ApplyUpdateInternal(latestRelease, false);
    }

    private async Task<UpdateResult> ApplyUpdateInternal(FwLiteRelease latestRelease, bool quitOnUpdate)
    {
        _logger.LogInformation("Installing new version: {Version}, Current version: {CurrentVersion}", latestRelease.Version, AppVersion.Version);
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
            NotifyInstallProgress(progressInfo.percentage, latestRelease);
            if (progressInfo.state == DeploymentProgressState.Queued)
            {
                _logger.LogInformation("Queued update");
                return;
            }
            _logger.LogInformation("Downloading update: {ProgressPercentage}%", progressInfo.percentage);
        };
        ShowUpdateInstallingNotification(latestRelease);

        //note this asyncOperation is not reliable, it's possible the update will install and this will never resolve
        var updateTask = asyncOperation.AsTask();
        var completedTask = await Task.WhenAny(updateTask, Task.Delay(TimeSpan.FromMinutes(2)));
        if (completedTask == updateTask)
        {
            var result = await updateTask;
            if (!string.IsNullOrEmpty(result.ErrorText))
            {
                _logger.LogError(result.ExtendedErrorCode, "Failed to download update: {ErrorText}", result.ErrorText);
                return UpdateResult.Failed;
            }

            _logger.LogInformation("Update downloaded, will install on next restart");
            return UpdateResult.Success;
        }

        return UpdateResult.Started;
    }

    private void NotifyInstallProgress(uint percentage, FwLiteRelease release)
    {
        _eventBus.PublishEvent(new AppUpdateProgressEvent(percentage, release));
    }

    public override bool SupportsAutoUpdate => !FwLiteMauiKernel.IsPortableApp;

    public override bool IsOnMeteredConnection()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile == null) return false;
        var cost = profile.GetConnectionCost();
        return cost.NetworkCostType != NetworkCostType.Unrestricted;
    }
}
