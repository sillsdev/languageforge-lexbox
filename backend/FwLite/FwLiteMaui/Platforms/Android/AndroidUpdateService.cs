using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using FwLiteShared;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;
using Xamarin.Google.Android.Play.Core.Install;
using Xamarin.Google.Android.Play.Core.Install.Model;

namespace FwLiteMaui;

/// <summary>
/// Android implementation of <see cref="IPlatformUpdateService"/> using Google Play In-App Updates.
/// Uses flexible updates by default (download in background, user can continue working).
/// High-priority updates use immediate flow (blocking full-screen UI).
/// See: https://developer.android.com/guide/playcore/in-app-updates
/// </summary>
public class AndroidUpdateService : MauiPlatformUpdateServiceBase, IMauiInitializeService, IInstallStateUpdatedListener
{
    /// <summary>
    /// Arbitrary request code used to identify the update flow result in OnActivityResult.
    /// The value itself doesn't matter, it just needs to be unique within the app.
    /// </summary>
    public const int UpdateRequestCode = 9001;

    /// <summary>
    /// Play Store listing URL. Used as the release URL for fallback scenarios
    /// (e.g., if in-app update fails and we need to direct user to the store).
    /// </summary>
    public const string PlayStoreUrl = "https://play.google.com/store/apps/details?id=org.sil.FwLiteMaui";

    private readonly ILogger<AndroidUpdateService> _logger;
    private readonly GlobalEventBus _eventBus;

    private IAppUpdateManager? _appUpdateManager;
    private TaskCompletionSource<UpdateResult>? _updateResultTcs;
    private AppUpdateInfo? _cachedUpdateInfo;

    public AndroidUpdateService(
        IHttpClientFactory httpClientFactory,
        IOptions<FwLiteConfig> config,
        ILogger<AndroidUpdateService> logger,
        IPreferences preferences,
        GlobalEventBus eventBus)
        : base(httpClientFactory, config, logger, preferences)
    {
        _logger = logger;
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

    public override bool SupportsAutoUpdate => true;

    /// <summary>
    /// Checks if the current network connection is metered (e.g., mobile data).
    /// MAUI's IConnectivity doesn't expose metered status, so we use Android's ConnectivityManager.
    /// </summary>
    public override bool IsOnMeteredConnection()
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

    /// <summary>
    /// Override to check for updates via Play Store instead of HTTP to LexBox/GitHub.
    /// </summary>
    public override async Task<ShouldUpdateResponse> ShouldUpdateAsync()
    {
        if (_appUpdateManager is null)
        {
            _logger.LogWarning("AppUpdateManager not initialized, cannot check for updates");
            return new ShouldUpdateResponse(null);
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
                    Url: PlayStoreUrl); // Fallback if in-app update fails

                return new ShouldUpdateResponse(release);
            }

            return new ShouldUpdateResponse(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates from Play Store");
            return new ShouldUpdateResponse(null);
        }
    }

    public override async Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease)
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

            // Determine update type based on in-app update priority (0-5, set in Play Console):
            // - Low priority (0-3): Flexible flow - downloads in background, user continues working
            // - High priority (4-5): Immediate flow - blocking full-screen update UI
            // See: https://developer.android.com/guide/playcore/in-app-updates#update-priority
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

    public override Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease)
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
                    new FwLiteRelease(_cachedUpdateInfo?.AvailableVersionCode().ToString() ?? "unknown", PlayStoreUrl)));
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
