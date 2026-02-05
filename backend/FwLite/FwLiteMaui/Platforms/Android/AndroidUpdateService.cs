using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;
using Xamarin.Google.Android.Play.Core.Install;
using Xamarin.Google.Android.Play.Core.Install.Model;

namespace FwLiteMaui;

/// <summary>
/// Android implementation of <see cref="IPlatformUpdateService"/> using Google Play In-App Updates.
/// Uses flexible updates by default (download in background, prompt to install).
/// Immediate updates (blocking full-screen UI) are triggered for priority >= 4.
/// </summary>
public class AndroidUpdateService : IPlatformUpdateService, IMauiInitializeService, IInstallStateUpdatedListener
{
    public const int UpdateRequestCode = 9001;

    private readonly ILogger<AndroidUpdateService> _logger;
    private readonly IPreferences _preferences;
    private readonly GlobalEventBus _eventBus;

    private IAppUpdateManager? _appUpdateManager;
    private TaskCompletionSource<UpdateResult>? _updateResultTcs;
    private AppUpdateInfo? _cachedUpdateInfo;

    private const string LastUpdateCheckKey = "lastUpdateChecked_android";

    public AndroidUpdateService(
        ILogger<AndroidUpdateService> logger,
        IPreferences preferences,
        GlobalEventBus eventBus)
    {
        _logger = logger;
        _preferences = preferences;
        _eventBus = eventBus;
    }

    public void Initialize(IServiceProvider services)
    {
        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            _logger.LogWarning("No current activity during AndroidUpdateService initialization");
            return;
        }

        _appUpdateManager = AppUpdateManagerFactory.Create(activity);
        _logger.LogInformation("AndroidUpdateService initialized with AppUpdateManager");
    }

    #region IPlatformUpdateService Implementation

    public bool HandlesOwnUpdateCheck => true;

    public bool SupportsAutoUpdate => true;

    public DateTime LastUpdateCheck
    {
        get => _preferences.Get(LastUpdateCheckKey, DateTime.MinValue);
        set => _preferences.Set(LastUpdateCheckKey, value);
    }

    public bool IsOnMeteredConnection()
    {
        var connectivityManager = Android.App.Application.Context.GetSystemService(Context.ConnectivityService)
            as Android.Net.ConnectivityManager;
        if (connectivityManager is null) return false;

        var network = connectivityManager.ActiveNetwork;
        if (network is null) return false;

        var capabilities = connectivityManager.GetNetworkCapabilities(network);
        if (capabilities is null) return false;

        // NOT_METERED capability means unlimited/unmetered connection
        return !capabilities.HasCapability(Android.Net.NetCapability.NotMetered);
    }

    public async Task<AvailableUpdate?> CheckForUpdateAsync()
    {
        if (_appUpdateManager is null)
        {
            _logger.LogWarning("AppUpdateManager not initialized, cannot check for updates");
            return null;
        }

        try
        {
            var appUpdateInfoTask = _appUpdateManager.AppUpdateInfo;
            var appUpdateInfo = await appUpdateInfoTask.AsAsync<AppUpdateInfo>();
            _cachedUpdateInfo = appUpdateInfo;

            var updateAvailability = appUpdateInfo.UpdateAvailability();
            _logger.LogInformation(
                "Play Store update check: availability={Availability}, versionCode={VersionCode}",
                updateAvailability, appUpdateInfo.AvailableVersionCode());

            if (updateAvailability == UpdateAvailability.UpdateAvailable ||
                updateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress)
            {
                // Use version code as version string since that's what Play Store provides
                var release = new FwLiteRelease(
                    Version: appUpdateInfo.AvailableVersionCode().ToString(),
                    Url: string.Empty); // Not needed for Play Store updates

                return new AvailableUpdate(release, SupportsAutoUpdate);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates from Play Store");
            return null;
        }
    }

    public async Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease)
    {
        if (_appUpdateManager is null)
        {
            _logger.LogWarning("AppUpdateManager not initialized, cannot apply update");
            return UpdateResult.Failed;
        }

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            _logger.LogWarning("No current activity, cannot start update flow");
            return UpdateResult.Failed;
        }

        try
        {
            // Get fresh update info if we don't have it cached
            var appUpdateInfo = _cachedUpdateInfo;
            if (appUpdateInfo is null)
            {
                var appUpdateInfoTask = _appUpdateManager.AppUpdateInfo;
                appUpdateInfo = await appUpdateInfoTask.AsAsync<AppUpdateInfo>();
            }

            // Determine update type based on priority:
            // Priority 0-3: Flexible (user can continue using app during download)
            // Priority 4-5: Immediate (blocking full-screen update)
            var priority = appUpdateInfo.UpdatePriority();
            var updateType = priority >= 4 ? AppUpdateType.Immediate : AppUpdateType.Flexible;

            _logger.LogInformation(
                "Starting {UpdateType} update flow (priority={Priority})",
                updateType == AppUpdateType.Immediate ? "immediate" : "flexible",
                priority);

            // Register listener for flexible updates to track progress
            if (updateType == AppUpdateType.Flexible)
            {
                _appUpdateManager.RegisterListener(this);
            }

            // Create completion source to await user response
            _updateResultTcs = new TaskCompletionSource<UpdateResult>();

            // Start the update flow
            var options = AppUpdateOptions.NewBuilder(updateType).Build();
            var startResult = _appUpdateManager.StartUpdateFlowForResult(
                appUpdateInfo,
                activity,
                options,
                UpdateRequestCode);

            if (startResult != 0)
            {
                _logger.LogWarning("StartUpdateFlowForResult returned non-zero: {Result}", startResult);
            }

            // Wait for activity result (from HandleActivityResult) or timeout
            var completedTask = await Task.WhenAny(
                _updateResultTcs.Task,
                Task.Delay(TimeSpan.FromMinutes(10)));

            if (completedTask != _updateResultTcs.Task)
            {
                _logger.LogWarning("Update flow timed out after 10 minutes");
                return UpdateResult.Started; // Assume it's running in background
            }

            return await _updateResultTcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start update flow");
            return UpdateResult.Failed;
        }
    }

    public Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease)
    {
        // On Android, the Play Store UI handles the permission flow
        return Task.FromResult(true);
    }

    #endregion

    #region Activity Result Handling

    /// <summary>
    /// Called from MainActivity.OnActivityResult when the update flow completes.
    /// </summary>
    public void HandleActivityResult(int requestCode, Result resultCode)
    {
        if (requestCode != UpdateRequestCode) return;

        var result = resultCode switch
        {
            Result.Ok => UpdateResult.Started, // Update accepted, will install
            Result.Canceled => UpdateResult.Disallowed, // User cancelled
            _ => UpdateResult.Failed // ActivityResult.ResultInAppUpdateFailed or other error
        };

        _logger.LogInformation("Update flow completed with result: {Result} (resultCode={ResultCode})",
            result, resultCode);

        _updateResultTcs?.TrySetResult(result);
    }

    #endregion

    #region IInstallStateUpdatedListener (for flexible updates)

    public void OnStateUpdate(InstallState state)
    {
        var status = state.InstallStatus();
        _logger.LogInformation("Install state update: status={Status}", status);

        switch (status)
        {
            case InstallStatus.Downloading:
                var bytesDownloaded = state.BytesDownloaded();
                var totalBytes = state.TotalBytesToDownload();
                var percentage = totalBytes > 0 ? (uint)(bytesDownloaded * 100 / totalBytes) : 0u;

                _eventBus.PublishEvent(new AppUpdateProgressEvent(
                    percentage,
                    new FwLiteRelease(_cachedUpdateInfo?.AvailableVersionCode().ToString() ?? "unknown", "")));
                break;

            case InstallStatus.Downloaded:
                // Flexible update downloaded, prompt user to restart
                _logger.LogInformation("Flexible update downloaded, completing update...");
                _appUpdateManager?.CompleteUpdate();
                break;

            case InstallStatus.Installed:
                _logger.LogInformation("Update installed successfully");
                _appUpdateManager?.UnregisterListener(this);
                break;

            case InstallStatus.Failed:
                _logger.LogWarning("Update installation failed");
                _appUpdateManager?.UnregisterListener(this);
                break;

            case InstallStatus.Canceled:
                _logger.LogInformation("Update installation was cancelled");
                _appUpdateManager?.UnregisterListener(this);
                break;
        }
    }

    #endregion

    #region Resume Handling

    /// <summary>
    /// Called from MainActivity.OnResume to check for downloaded-but-not-installed updates.
    /// </summary>
    public async Task CheckForPendingInstallAsync()
    {
        if (_appUpdateManager is null) return;

        try
        {
            var appUpdateInfoTask = _appUpdateManager.AppUpdateInfo;
            var appUpdateInfo = await appUpdateInfoTask.AsAsync<AppUpdateInfo>();

            if (appUpdateInfo.InstallStatus() == InstallStatus.Downloaded)
            {
                _logger.LogInformation("Found downloaded update on resume, completing installation");
                _appUpdateManager.CompleteUpdate();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for pending install on resume");
        }
    }

    #endregion
}
