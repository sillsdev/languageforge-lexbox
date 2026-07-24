using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using FwLiteShared.Services;
using Microsoft.Extensions.Logging;
using AndroidApplication = Android.App.Application;

namespace FwLiteMaui;

public sealed class AndroidForegroundWorkHost(ILogger<AndroidForegroundWorkHost> logger) : ILongRunningWorkHost
{
    private const int NotificationPermissionRequestCode = 2300;
    private readonly Lock wakeLockLock = new();
    private PowerManager.WakeLock? wakeLock;

    public Task WorkStartedAsync(LongRunningWorkRequest request, CancellationToken cancellationToken)
    {
        var context = Platform.AppContext ?? AndroidApplication.Context;
        RequestNotificationPermissionIfNeeded(context);

        var intent = new Intent(context, typeof(ForegroundWorkService))
            .PutExtra(ForegroundWorkService.TitleExtra, request.Title)
            .PutExtra(ForegroundWorkService.TextExtra, request.NotificationText);
        ContextCompat.StartForegroundService(context, intent);
        AcquireWakeLock(context);
        return Task.CompletedTask;
    }

    public Task WorkQueueDrainedAsync(CancellationToken cancellationToken)
    {
        ReleaseWakeLock();

        var context = Platform.AppContext ?? AndroidApplication.Context;
        context.StopService(new Intent(context, typeof(ForegroundWorkService)));
        return Task.CompletedTask;
    }

    private void RequestNotificationPermissionIfNeeded(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return;
        if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) == Permission.Granted) return;

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            logger.LogWarning("Unable to request Android notification permission because no current activity is available");
            return;
        }

        ActivityCompat.RequestPermissions(
            activity,
            [Manifest.Permission.PostNotifications],
            NotificationPermissionRequestCode);
    }

    private void AcquireWakeLock(Context context)
    {
        lock (wakeLockLock)
        {
            if (wakeLock?.IsHeld == true) return;

            var powerManager = (PowerManager?)context.GetSystemService(Context.PowerService);
            wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, "FwLite:LongRunningWork");
            wakeLock?.Acquire();
        }
    }

    private void ReleaseWakeLock()
    {
        lock (wakeLockLock)
        {
            try
            {
                if (wakeLock?.IsHeld == true) wakeLock.Release();
            }
            finally
            {
                wakeLock?.Dispose();
                wakeLock = null;
            }
        }
    }
}
