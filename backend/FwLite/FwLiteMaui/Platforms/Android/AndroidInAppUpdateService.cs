using Android.App;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;

namespace FwLiteMaui;

/// <summary>
/// Drives Google Play's In-App Updates "flexible" flow on Android. On launch we ask Play whether an
/// update is available; if so, Play renders its own bottom-sheet prompt and downloads the update in the
/// background while the user keeps working. When the download has finished we raise an
/// <see cref="AppUpdateEvent"/> so the web UI can prompt the user to restart, which calls back into
/// <see cref="RestartToApplyUpdate"/>.
///
/// The availability check is gated on the same interval as the other platforms via the shared
/// <see cref="UpdateCheckThrottle"/>, so we don't re-surface the bottom sheet on every launch/resume.
///
/// Downloaded-state detection is done by polling <c>AppUpdateInfo.InstallStatus()</c> on resume rather
/// than by registering an <c>IInstallStateUpdatedListener</c>: the C# binding's generic
/// <c>OnStateUpdate(InstallState)</c> has a name-clash that fails to compile without a Java-source
/// wrapper (dotnet/android-libraries#1006), and polling on resume is a documented alternative for the
/// flexible flow. The tradeoff is no live in-app progress bar — acceptable since Play shows its own UI.
///
/// This only does anything for builds installed from Google Play; for sideloaded/debug builds Play
/// reports "no update available" and every call here is a harmless no-op.
/// </summary>
public sealed class AndroidInAppUpdateService : IPlatformUpdateService, IDisposable
{
    /// <summary>Activity-result request code for the Play update flow (see <see cref="MainActivity"/>).</summary>
    public const int UpdateRequestCode = 2400;

    private readonly IAppUpdateManager _appUpdateManager;
    private readonly ILogger<AndroidInAppUpdateService> _logger;
    private readonly UpdateCheckThrottle _throttle;
    private readonly GlobalEventBus _eventBus;

    // Notify about a downloaded update at most once per app run, so tapping "Later"/ignoring the toast
    // doesn't re-prompt on every OnResume.
    private bool _downloadedNotified;

    public AndroidInAppUpdateService(
        ILogger<AndroidInAppUpdateService> logger,
        UpdateCheckThrottle throttle,
        GlobalEventBus eventBus)
    {
        _logger = logger;
        _throttle = throttle;
        _eventBus = eventBus;
        _appUpdateManager = AppUpdateManagerFactory.Create(Platform.AppContext);
    }

    /// <summary>
    /// Kick off a launch-time check. If a flexible update is available and the 8h timer allows, start
    /// Play's update flow (bottom sheet). If one was already downloaded, notify the UI to prompt a restart.
    /// </summary>
    public void CheckForUpdate(Activity activity)
    {
        QueryInfo(info =>
        {
            //A downloaded update is ready to install regardless of the timer - surface it.
            if (info.InstallStatus() == InstallStatus.Downloaded)
            {
                NotifyDownloaded(info);
                return;
            }

            if (!_throttle.ShouldCheckForUpdate()) return;
            _throttle.RecordCheck();

            if (info.UpdateAvailability() == UpdateAvailability.UpdateAvailable &&
                info.IsUpdateTypeAllowed(AppUpdateType.Flexible))
            {
                StartFlexibleUpdate(info, activity);
            }
        });
    }

    /// <summary>
    /// Re-check on resume for a flexible update whose download finished while we weren't in the
    /// foreground, and notify the UI to prompt a restart.
    /// </summary>
    public void CheckForDownloadedUpdateOnResume()
    {
        QueryInfo(info =>
        {
            if (info.InstallStatus() == InstallStatus.Downloaded)
            {
                NotifyDownloaded(info);
            }
        });
    }

    private void NotifyDownloaded(AppUpdateInfo info)
    {
        if (_downloadedNotified) return;
        _downloadedNotified = true;
        //Play flexible updates have no GitHub release; the version code is all we know and the toast
        //doesn't display it, so a minimal release is fine.
        var release = new FwLiteRelease(info.AvailableVersionCode().ToString(), string.Empty);
        //Success is the shared "downloaded/staged, restart to finish installing" state; the web UI's
        //Success toast offers Restart, which calls back into RestartToApplyUpdate below.
        _eventBus.PublishEvent(new AppUpdateEvent(UpdateResult.Success, release));
    }

    private void QueryInfo(Action<AppUpdateInfo> onInfo)
    {
        try
        {
            _ = _appUpdateManager.GetAppUpdateInfo()
                .AddOnSuccessListener(new OnSuccessListener(result =>
                {
                    if (result is AppUpdateInfo info)
                    {
                        try
                        {
                            onInfo(info);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Failed handling Play app-update info");
                        }
                    }
                }))
                .AddOnFailureListener(new OnFailureListener(e =>
                    _logger.LogInformation(e, "Play app-update info unavailable (expected off-Play)")));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to query Play for app updates");
        }
    }

    private void StartFlexibleUpdate(AppUpdateInfo info, Activity activity)
    {
        try
        {
            var options = AppUpdateOptions.NewBuilder(AppUpdateType.Flexible).Build();
            if (_appUpdateManager.StartUpdateFlowForResult(info, activity, options, UpdateRequestCode))
            {
                _logger.LogInformation("Started Play flexible in-app update flow");
            }
            else
            {
                _logger.LogWarning("Play did not start the flexible in-app update flow");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start Play in-app update flow");
        }
    }

    // IPlatformUpdateService

    //Play in-app updates aren't app-driven downloads, so the metered-connection prompt doesn't apply;
    //Play handles data-usage consent in its own UI.
    public bool IsOnMeteredConnection() => false;

    //Updating is done through Play's own flow (CheckForUpdate + RestartToApplyUpdate), not the shared
    //ApplyUpdate path, so the manual Updates dialog keeps showing "Download" (linking to Play).
    public bool SupportsAutoUpdate => false;

    public Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease) => Task.FromResult(UpdateResult.Unknown);

    public Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease) => Task.FromResult(true);

    public Task RestartToApplyUpdate()
    {
        try
        {
            //Play's completeUpdate() installs the downloaded flexible update and restarts the app. It
            //returns a Play Task, not an awaitable; log if Play rejects the completion.
            _ = _appUpdateManager.CompleteUpdate()
                .AddOnFailureListener(new OnFailureListener(e =>
                    _logger.LogError(e, "Failed to complete Play in-app update")));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to complete Play in-app update");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _appUpdateManager.Dispose();
    }

    private sealed class OnSuccessListener(Action<Java.Lang.Object?> onSuccess)
        : Java.Lang.Object, Android.Gms.Tasks.IOnSuccessListener
    {
        public void OnSuccess(Java.Lang.Object? result) => onSuccess(result);
    }

    private sealed class OnFailureListener(Action<Java.Lang.Exception> onFailure)
        : Java.Lang.Object, Android.Gms.Tasks.IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception e) => onFailure(e);
    }
}
