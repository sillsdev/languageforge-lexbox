using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Microsoft.Extensions.Logging;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;

namespace FwLiteMaui;

/// <summary>
/// Drives Google Play's In-App Updates "flexible" flow on Android. On launch we ask Play whether an
/// update is available; if so, Play renders its own bottom-sheet prompt and downloads the update in the
/// background while the user keeps working. When the download has finished we prompt the user to restart
/// and install.
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
public sealed class AndroidInAppUpdateService : IDisposable
{
    /// <summary>Activity-result request code for the Play update flow (see <see cref="MainActivity"/>).</summary>
    public const int UpdateRequestCode = 2400;

    private readonly IAppUpdateManager _appUpdateManager;
    private readonly ILogger _logger;

    public AndroidInAppUpdateService(Context context, ILogger logger)
    {
        _logger = logger;
        _appUpdateManager = AppUpdateManagerFactory.Create(context);
    }

    /// <summary>
    /// Kick off a launch-time check. If a flexible update is available, start Play's update flow (bottom
    /// sheet). If one was already downloaded on a previous run, prompt to complete it.
    /// </summary>
    public void CheckForUpdate(Activity activity)
    {
        QueryInfo(info =>
        {
            if (info.InstallStatus() == InstallStatus.Downloaded)
            {
                PromptCompleteUpdate();
                return;
            }

            if (info.UpdateAvailability() == UpdateAvailability.UpdateAvailable &&
                info.IsUpdateTypeAllowed(AppUpdateType.Flexible))
            {
                StartFlexibleUpdate(info, activity);
            }
        });
    }

    /// <summary>
    /// Re-check on resume for a flexible update whose download finished while we weren't in the
    /// foreground, and prompt to install it.
    /// </summary>
    public void CheckForDownloadedUpdateOnResume()
    {
        QueryInfo(info =>
        {
            if (info.InstallStatus() == InstallStatus.Downloaded)
            {
                PromptCompleteUpdate();
            }
        });
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

    private void PromptCompleteUpdate()
    {
        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            _logger.LogInformation("Update downloaded but no current activity to prompt on; will retry on next resume");
            return;
        }

        new AlertDialog.Builder(activity)
            .SetTitle("Update ready")!
            .SetMessage("A new version of FieldWorks Lite has been downloaded. Restart to finish installing.")!
            .SetPositiveButton("Restart & install", (_, _) =>
            {
                //CompleteUpdate returns a Play Task, not an awaitable; log if Play rejects the completion.
                _ = _appUpdateManager.CompleteUpdate()
                    .AddOnFailureListener(new OnFailureListener(e =>
                        _logger.LogError(e, "Failed to complete Play in-app update")));
            })!
            .SetNegativeButton("Later", (_, _) => { })!
            .Show();
    }

    public void Dispose()
    {
        _appUpdateManager.Dispose();
    }

    private sealed class OnSuccessListener(Action<Java.Lang.Object?> onSuccess) : Java.Lang.Object, IOnSuccessListener
    {
        public void OnSuccess(Java.Lang.Object? result) => onSuccess(result);
    }

    private sealed class OnFailureListener(Action<Java.Lang.Exception> onFailure) : Java.Lang.Object, IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception e) => onFailure(e);
    }
}
